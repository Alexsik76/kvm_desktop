using Avalonia.Input;

namespace KvmDesktop.Services;

public class AvaloniaEventMapper : IEventMapper
{
    public byte AvaloniaToHidKey(Key key)
    {
        return key switch
        {
            Key.A => 0x04,
            Key.B => 0x05,
            Key.C => 0x06,
            Key.D => 0x07,
            Key.E => 0x08,
            Key.F => 0x09,
            Key.G => 0x0A,
            Key.H => 0x0B,
            Key.I => 0x0C,
            Key.J => 0x0D,
            Key.K => 0x0E,
            Key.L => 0x0F,
            Key.M => 0x10,
            Key.N => 0x11,
            Key.O => 0x12,
            Key.P => 0x13,
            Key.Q => 0x14,
            Key.R => 0x15,
            Key.S => 0x16,
            Key.T => 0x17,
            Key.U => 0x18,
            Key.V => 0x19,
            Key.W => 0x1A,
            Key.X => 0x1B,
            Key.Y => 0x1C,
            Key.Z => 0x1D,

            Key.D1 => 0x1E,
            Key.D2 => 0x1F,
            Key.D3 => 0x20,
            Key.D4 => 0x21,
            Key.D5 => 0x22,
            Key.D6 => 0x23,
            Key.D7 => 0x24,
            Key.D8 => 0x25,
            Key.D9 => 0x26,
            Key.D0 => 0x27,

            Key.Enter => 0x28,
            Key.Escape => 0x29,
            Key.Back => 0x2A, // Backspace
            Key.Tab => 0x2B,
            Key.Space => 0x2C,

            Key.OemMinus => 0x2D, // -/_
            Key.OemPlus => 0x2E,  // =/+
            Key.OemOpenBrackets => 0x2F, // [/{
            Key.OemCloseBrackets => 0x30, // ]/}
            Key.OemBackslash => 0x31,    // \ / |
            // Key.OemTilde is often used for #/~ in UK layouts, 
            // but in US HID it is 0x35 for `/~
            Key.OemSemicolon => 0x33, // ;/:
            Key.OemQuotes => 0x34,    // ' / "
            Key.OemTilde => 0x35,     // ` / ~
            Key.OemComma => 0x36,     // , / <
            Key.OemPeriod => 0x37,    // . / >
            Key.OemQuestion => 0x38,  // / / ?

            Key.CapsLock => 0x39,

            Key.F1 => 0x3A,
            Key.F2 => 0x3B,
            Key.F3 => 0x3C,
            Key.F4 => 0x3D,
            Key.F5 => 0x3E,
            Key.F6 => 0x3F,
            Key.F7 => 0x40,
            Key.F8 => 0x41,
            Key.F9 => 0x42,
            Key.F10 => 0x43,
            Key.F11 => 0x44,
            Key.F12 => 0x45,

            Key.PrintScreen => 0x46,
            Key.Scroll => 0x47, // Scroll Lock
            Key.Pause => 0x48,
            Key.Insert => 0x49,
            Key.Home => 0x4A,
            Key.PageUp => 0x4B,
            Key.Delete => 0x4C,
            Key.End => 0x4D,
            Key.PageDown => 0x4E,

            Key.Right => 0x4F,
            Key.Left => 0x50,
            Key.Down => 0x51,
            Key.Up => 0x52,

            Key.NumLock => 0x53,
            
            // Keypad numbers
            Key.NumPad1 => 0x1E, // Mapped to normal 1 for simplicity if host expects HID keyboard report
            // but usually HID has separate codes for numpad
            // Keypad 1 is 0x59
            Key.NumPad2 => 0x5A,
            Key.NumPad3 => 0x5B,
            Key.NumPad4 => 0x5C,
            Key.NumPad5 => 0x5D,
            Key.NumPad6 => 0x5E,
            Key.NumPad7 => 0x5F,
            Key.NumPad8 => 0x60,
            Key.NumPad9 => 0x61,
            Key.NumPad0 => 0x62,
            
            Key.Divide => 0x54, // Numpad /
            Key.Multiply => 0x55, // Numpad *
            Key.Subtract => 0x56, // Numpad -
            Key.Add => 0x57, // Numpad +
            // Key.Separator => 0x58, // Numpad Enter?

            _ => 0
        };
    }

    public byte AvaloniaToHidModifiers(KeyModifiers modifiers)
    {
        byte hidMod = 0;
        // USB HID modifiers:
        // LCtrl: 0x01 (bit 0)
        // LShift: 0x02 (bit 1)
        // LAlt: 0x04 (bit 2)
        // LGUI (Win): 0x08 (bit 3)
        // RCtrl: 0x10 (bit 4)
        // RShift: 0x20 (bit 5)
        // RAlt: 0x40 (bit 6)
        // RGUI (Win): 0x80 (bit 7)
        
        // Avalonia KeyModifiers doesn't distinguish L/R by default in event.Modifiers.
        // It's a bitmask: Control, Shift, Alt, Windows, Meta.
        
        if (modifiers.HasFlag(KeyModifiers.Control)) hidMod |= 0x01; // Default to Left
        if (modifiers.HasFlag(KeyModifiers.Shift))   hidMod |= 0x02; // Default to Left
        if (modifiers.HasFlag(KeyModifiers.Alt))     hidMod |= 0x04; // Default to Left
        if (modifiers.HasFlag(KeyModifiers.Meta))    hidMod |= 0x08; // Default to Left (Windows key)
        
        return hidMod;
    }
}
