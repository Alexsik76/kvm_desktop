using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace KvmDesktop.Services;

public partial class InputCapturer : IInputCapturer
{
    private readonly IHidClient _hidClient;
    private Control? _control;
    private bool _isEnabled;
    private byte _mouseButtons = 0;
    private Point _lastPointerPosition;
    private bool _isFirstMove = true;
    private bool _isInternalMove = false;

    private double _accDx = 0;
    private double _accDy = 0;
    private int _accWheel = 0;
    private byte _lastButtons = 0;
    private Avalonia.Threading.DispatcherTimer? _flushTimer;

    // ── Low-level keyboard hook ──────────────────────────────────────────────
    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;        // held as a field so GC does not collect the delegate
    private readonly HashSet<uint> _pressedVkCodes = new();

    private const int WH_KEYBOARD_LL  = 13;
    private const int WM_KEYDOWN      = 0x0100;
    private const int WM_SYSKEYDOWN   = 0x0104;
    private const uint VK_F11         = 0x7A;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // ── P/Invoke ─────────────────────────────────────────────────────────────
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    // ── Public surface ────────────────────────────────────────────────────────
    public event EventHandler? CaptureReleased;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            UpdateCaptureState();
        }
    }

    public InputCapturer(IHidClient hidClient)
    {
        _hidClient = hidClient;
    }

    public void Attach(Control control)
    {
        if (_control != null) Detach();

        _control = control;
        _control.PointerMoved += OnPointerMoved;
        _control.PointerPressed += OnPointerPressed;
        _control.PointerReleased += OnPointerReleased;
        _control.PointerWheelChanged += OnPointerWheelChanged;

        _flushTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 Hz
        };
        _flushTimer.Tick += OnFlushTick;
        _flushTimer.Start();
    }

    public void Detach()
    {
        if (_control == null) return;

        _control.PointerMoved -= OnPointerMoved;
        _control.PointerPressed -= OnPointerPressed;
        _control.PointerReleased -= OnPointerReleased;
        _control.PointerWheelChanged -= OnPointerWheelChanged;

        if (_flushTimer != null)
        {
            _flushTimer.Stop();
            _flushTimer.Tick -= OnFlushTick;
            _flushTimer = null;
        }
        _accDx = 0;
        _accDy = 0;
        _accWheel = 0;

        _isEnabled = false;
        UpdateCaptureState();
        _control = null;
    }

    // ── Capture state ─────────────────────────────────────────────────────────
    private void UpdateCaptureState()
    {
        if (_isEnabled)
        {
            if (_control == null) return;
            _control.Cursor = new Cursor(StandardCursorType.None);
            _isFirstMove = true;
            _control.Focus();
            InstallHook();
        }
        else
        {
            UninstallHook();
            SendAllKeysUp();
            if (_control != null) _control.Cursor = Cursor.Default;
        }
    }

    private void InstallHook()
    {
        if (_hookHandle != IntPtr.Zero) return;
        _hookProc = HookCallback;
        // hMod is ignored for WH_KEYBOARD_LL (global, no DLL injection)
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, IntPtr.Zero, 0);
    }

    private void UninstallHook()
    {
        if (_hookHandle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        _pressedVkCodes.Clear();
    }

    private void SendAllKeysUp()
    {
        _pressedVkCodes.Clear();
        _hidClient.EnqueueKeyboardEvent(0, []);
    }

    // ── Low-level keyboard hook callback ──────────────────────────────────────
    // Runs on the UI/message-pump thread. Must return quickly.
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !_isEnabled)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        uint vk = kbd.vkCode;
        long msg = wParam.ToInt64();
        bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;

        // F11 toggles capture off; never forward it to the remote host.
        if (vk == VK_F11)
        {
            if (isDown)
            {
                _isEnabled = false;
                _pressedVkCodes.Clear();
                // Schedule unhook+event on next dispatcher tick — safe to call from inside a hook callback.
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateCaptureState();
                    CaptureReleased?.Invoke(this, EventArgs.Empty);
                });
            }
            return (IntPtr)1; // swallow F11 (both keydown and keyup)
        }

        if (isDown)
            _pressedVkCodes.Add(vk);
        else
            _pressedVkCodes.Remove(vk);

        SendHookKeyboardReport();
        return (IntPtr)1; // swallow; Windows desktop never sees the key
    }

    private void SendHookKeyboardReport()
    {
        byte modifiers = 0;
        var keyCodes = new List<byte>(6);

        foreach (uint vk in _pressedVkCodes)
        {
            byte mod = VkToHidModifier(vk);
            if (mod != 0)
            {
                modifiers |= mod;
            }
            else
            {
                byte hid = VkToHidKey(vk);
                if (hid != 0 && keyCodes.Count < 6)
                    keyCodes.Add(hid);
            }
        }

        _hidClient.EnqueueKeyboardEvent(modifiers, [.. keyCodes]);
    }

    // VK modifier byte — mirrors AvaloniaEventMapper but uses raw VK codes.
    private static byte VkToHidModifier(uint vk) => vk switch
    {
        0x10 or 0xA0 => 0x02,   // Shift / LShift   → HID LShift
        0xA1         => 0x20,   // RShift            → HID RShift
        0x11 or 0xA2 => 0x01,   // Control / LCtrl  → HID LCtrl
        0xA3         => 0x10,   // RCtrl             → HID RCtrl
        0x12 or 0xA4 => 0x04,   // Menu / LAlt       → HID LAlt
        0xA5         => 0x40,   // RAlt              → HID RAlt
        0x5B         => 0x08,   // LWin              → HID LGUI
        0x5C         => 0x80,   // RWin              → HID RGUI
        _            => 0
    };

    // VK keycode → HID usage (page 07). Returns 0 for unknown/modifier keys.
    private static byte VkToHidKey(uint vk) => vk switch
    {
        >= 0x41 and <= 0x5A => (byte)(vk - 0x3D),    // A–Z  (VK_A=0x41 → 0x04)
        >= 0x31 and <= 0x39 => (byte)(vk - 0x13),    // 1–9  (VK_1=0x31 → 0x1E)
        0x30 => 0x27,   // 0
        0x0D => 0x28,   // Enter
        0x1B => 0x29,   // Escape
        0x08 => 0x2A,   // Backspace
        0x09 => 0x2B,   // Tab
        0x20 => 0x2C,   // Space
        0xBD => 0x2D,   // - _
        0xBB => 0x2E,   // = +
        0xDB => 0x2F,   // [ {
        0xDD => 0x30,   // ] }
        0xDC => 0x31,   // \ |
        0xBA => 0x33,   // ; :
        0xDE => 0x34,   // ' "
        0xC0 => 0x35,   // ` ~
        0xBC => 0x36,   // , <
        0xBE => 0x37,   // . >
        0xBF => 0x38,   // / ?
        0x14 => 0x39,   // CapsLock
        >= 0x70 and <= 0x7B => (byte)(vk - 0x36),    // F1–F12 (VK_F1=0x70 → 0x3A)
        0x2C => 0x46,   // PrintScreen
        0x91 => 0x47,   // ScrollLock
        0x13 => 0x48,   // Pause
        0x2D => 0x49,   // Insert
        0x24 => 0x4A,   // Home
        0x21 => 0x4B,   // PageUp
        0x2E => 0x4C,   // Delete
        0x23 => 0x4D,   // End
        0x22 => 0x4E,   // PageDown
        0x27 => 0x4F,   // Right
        0x25 => 0x50,   // Left
        0x28 => 0x51,   // Down
        0x26 => 0x52,   // Up
        0x90 => 0x53,   // NumLock
        0x6F => 0x54,   // Numpad /
        0x6A => 0x55,   // Numpad *
        0x6D => 0x56,   // Numpad -
        0x6B => 0x57,   // Numpad +
        0x61 => 0x59,   // Numpad 1
        0x62 => 0x5A,   // Numpad 2
        0x63 => 0x5B,   // Numpad 3
        0x64 => 0x5C,   // Numpad 4
        0x65 => 0x5D,   // Numpad 5
        0x66 => 0x5E,   // Numpad 6
        0x67 => 0x5F,   // Numpad 7
        0x68 => 0x60,   // Numpad 8
        0x69 => 0x61,   // Numpad 9
        0x60 => 0x62,   // Numpad 0
        0x6E => 0x63,   // Numpad .
        _    => 0
    };

    // ── Pointer handlers ──────────────────────────────────────────────────────
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isEnabled || _control == null || _isInternalMove) return;

        var currentPos = e.GetPosition(_control);

        if (_isFirstMove)
        {
            _lastPointerPosition = currentPos;
            _isFirstMove = false;
            return;
        }

        double dx = currentPos.X - _lastPointerPosition.X;
        double dy = currentPos.Y - _lastPointerPosition.Y;

        _accDx += dx;
        _accDy += dy;
        _lastPointerPosition = currentPos;

        _lastButtons = _mouseButtons;
    }

    private void OnFlushTick(object? sender, EventArgs e)
    {
        if (!_isEnabled || _control == null) return;

        if (Math.Abs(_accDx) < 1 && Math.Abs(_accDy) < 1 && _accWheel == 0)
        {
            return;
        }

        const int MAX_CHUNKS_PER_TICK = 16;
        int chunks = 0;

        while (chunks < MAX_CHUNKS_PER_TICK &&
               (Math.Abs(_accDx) >= 1 || Math.Abs(_accDy) >= 1 || _accWheel != 0))
        {
            sbyte hidX = (sbyte)Math.Clamp((int)Math.Round(_accDx), -127, 127);
            sbyte hidY = (sbyte)Math.Clamp((int)Math.Round(_accDy), -127, 127);
            sbyte hidW = (sbyte)Math.Clamp(_accWheel, -127, 127);

            if (hidX != 0 || hidY != 0 || hidW != 0)
            {
                _hidClient.EnqueueMouseEvent(_lastButtons, hidX, hidY, hidW);
            }

            _accDx -= hidX;
            _accDy -= hidY;
            _accWheel -= hidW;
            chunks++;
        }

        ResetCursorToCenter();
    }

    private unsafe void ResetCursorToCenter()
    {
        if (_control == null) return;

        var visual = _control.GetVisualRoot() as Window;
        if (visual == null) return;

        var centerLocal = new Point(_control.Bounds.Width / 2, _control.Bounds.Height / 2);

        _isInternalMove = true;
        try
        {
            var centerScreen = _control.PointToScreen(centerLocal);
            SetCursorPos(centerScreen.X, centerScreen.Y);
            _lastPointerPosition = centerLocal;
        }
        finally
        {
            _isInternalMove = false;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isEnabled) return;

        var props = e.GetCurrentPoint(_control).Properties;
        UpdateMouseButtons(props);
        _hidClient.EnqueueMouseEvent(_mouseButtons, 0, 0, 0);
        e.Handled = true;

        _control?.Focus();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isEnabled) return;

        var props = e.GetCurrentPoint(_control).Properties;
        UpdateMouseButtons(props);
        _hidClient.EnqueueMouseEvent(_mouseButtons, 0, 0, 0);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!_isEnabled) return;

        _accWheel += (int)Math.Clamp(e.Delta.Y, -127, 127);
        e.Handled = true;
    }

    private void UpdateMouseButtons(PointerPointProperties props)
    {
        _mouseButtons = 0;
        if (props.IsLeftButtonPressed)   _mouseButtons |= 0x01;
        if (props.IsRightButtonPressed)  _mouseButtons |= 0x02;
        if (props.IsMiddleButtonPressed) _mouseButtons |= 0x04;
    }
}
