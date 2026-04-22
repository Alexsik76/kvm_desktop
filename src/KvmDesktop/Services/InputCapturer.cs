using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Runtime.InteropServices;

namespace KvmDesktop.Services;

public partial class InputCapturer : IInputCapturer
{
    private readonly IHidClient _hidClient;
    private readonly IEventMapper _eventMapper;
    private Control? _control;
    private bool _isEnabled;
    private readonly List<Key> _pressedKeys = new();
    private byte _mouseButtons = 0;
    private Point _lastPointerPosition;
    private bool _isFirstMove = true;
    private bool _isInternalMove = false;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetCursorPos(int x, int y);

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

    public InputCapturer(IHidClient hidClient, IEventMapper eventMapper)
    {
        _hidClient = hidClient;
        _eventMapper = eventMapper;
    }

    public void Attach(Control control)
    {
        if (_control != null) Detach();
        
        _control = control;
        _control.KeyDown += OnKeyDown;
        _control.KeyUp += OnKeyUp;
        _control.PointerMoved += OnPointerMoved;
        _control.PointerPressed += OnPointerPressed;
        _control.PointerReleased += OnPointerReleased;
        _control.PointerWheelChanged += OnPointerWheelChanged;
    }

    public void Detach()
    {
        if (_control == null) return;
        
        _control.KeyDown -= OnKeyDown;
        _control.KeyUp -= OnKeyUp;
        _control.PointerMoved -= OnPointerMoved;
        _control.PointerPressed -= OnPointerPressed;
        _control.PointerReleased -= OnPointerReleased;
        _control.PointerWheelChanged -= OnPointerWheelChanged;
        
        _isEnabled = false;
        UpdateCaptureState();
        _control = null;
    }

    private void UpdateCaptureState()
    {
        if (_control == null) return;

        if (_isEnabled)
        {
            _control.Cursor = new Cursor(StandardCursorType.None);
            _isFirstMove = true;
            _control.Focus();
        }
        else
        {
            _control.Cursor = Cursor.Default;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isEnabled) return;

        if (e.Key == Key.F11)
        {
            _isEnabled = false;
            _pressedKeys.Clear();
            UpdateCaptureState();
            CaptureReleased?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (!_pressedKeys.Contains(e.Key))
        {
            _pressedKeys.Add(e.Key);
        }
        SendKeyboardReport(e.KeyModifiers);
        e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!_isEnabled) return;
        
        _pressedKeys.Remove(e.Key);
        SendKeyboardReport(e.KeyModifiers);
        e.Handled = true;
    }

    private void SendKeyboardReport(KeyModifiers modifiers)
    {
        byte hidMod = _eventMapper.AvaloniaToHidModifiers(modifiers);
        
        byte[] hidKeys = _pressedKeys
            .Take(6)
            .Select(k => _eventMapper.AvaloniaToHidKey(k))
            .Where(k => k != 0)
            .ToArray();
            
        _hidClient.EnqueueKeyboardEvent(hidMod, hidKeys);
    }

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

        // Convert to HID units (sbyte)
        sbyte hidX = (sbyte)Math.Clamp(dx, -127, 127);
        sbyte hidY = (sbyte)Math.Clamp(dy, -127, 127);

        // Only send if there's actual movement in HID units
        if (hidX != 0 || hidY != 0)
        {
            _hidClient.EnqueueMouseEvent(_mouseButtons, hidX, hidY, 0);
            ResetCursorToCenter();
        }
    }

    private unsafe void ResetCursorToCenter()
    {
        if (_control == null) return;
        
        var visual = _control.GetVisualRoot() as Window;
        if (visual == null) return;

        // Center of the control in DIPs
        var centerLocal = new Point(_control.Bounds.Width / 2, _control.Bounds.Height / 2);
        
        // Use a flag to ignore the movement event triggered by SetCursorPos
        _isInternalMove = true;
        try 
        {
            // PointToScreen converts DIPs to physical pixels (PixelPoint)
            var centerScreen = _control.PointToScreen(centerLocal);
            SetCursorPos(centerScreen.X, centerScreen.Y);
            
            // We update _lastPointerPosition to where we just set the hardware cursor.
            // This ensures the next dx/dy is calculated correctly from the center.
            _lastPointerPosition = centerLocal;
        }
        finally
        {
            // Reset the flag. Even if SetCursorPos is async, the next event will arrive
            // and we'll check if it's close to centerLocal.
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
        
        // Ensure focus
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
        
        sbyte wheel = (sbyte)Math.Clamp(e.Delta.Y, -127, 127);
        _hidClient.EnqueueMouseEvent(_mouseButtons, 0, 0, wheel);
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
