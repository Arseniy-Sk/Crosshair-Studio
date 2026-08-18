using Avalonia.Input;

namespace CrosshairStudio.Infrastructure;

public static class HotkeyMap
{
    public static bool IsModifier(Key key) => key is Key.LeftShift or Key.RightShift
        or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
        or Key.LWin or Key.RWin;

    public static int ToVirtualKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
            return 0x41 + (key - Key.A);
        if (key is >= Key.D0 and <= Key.D9)
            return 0x30 + (key - Key.D0);
        if (key is >= Key.F1 and <= Key.F24)
            return 0x70 + (key - Key.F1);
        if (key is >= Key.NumPad0 and <= Key.NumPad9)
            return 0x60 + (key - Key.NumPad0);

        return key switch
        {
            Key.OemTilde or Key.Oem3 => 0xC0,
            Key.OemMinus => 0xBD,
            Key.OemPlus => 0xBB,
            Key.OemOpenBrackets => 0xDB,
            Key.OemCloseBrackets => 0xDD,
            Key.OemPipe or Key.Oem5 => 0xDC,
            Key.OemSemicolon => 0xBA,
            Key.OemQuotes => 0xDE,
            Key.OemComma => 0xBC,
            Key.OemPeriod => 0xBE,
            Key.OemQuestion or Key.Oem2 => 0xBF,
            Key.Space => 0x20,
            Key.Tab => 0x09,
            Key.Enter => 0x0D,
            Key.Insert => 0x2D,
            Key.Delete => 0x2E,
            Key.Home => 0x24,
            Key.End => 0x23,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.Left => 0x25,
            Key.Up => 0x26,
            Key.Right => 0x27,
            Key.Down => 0x28,
            Key.Add => 0x6B,
            Key.Subtract => 0x6D,
            Key.Multiply => 0x6A,
            Key.Divide => 0x6F,
            Key.Decimal => 0x6E,
            Key.Oem8 => 0xDF,
            _ => 0
        };
    }

    public static string ToName(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
            return key.ToString();
        if (key is >= Key.D0 and <= Key.D9)
            return ((char)('0' + (key - Key.D0))).ToString();
        if (key is >= Key.F1 and <= Key.F24)
            return key.ToString();
        if (key is >= Key.NumPad0 and <= Key.NumPad9)
            return "Num " + (key - Key.NumPad0);

        return key switch
        {
            Key.OemTilde or Key.Oem3 => "`",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe or Key.Oem5 => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion or Key.Oem2 => "/",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Enter => "Enter",
            Key.Insert => "Ins",
            Key.Delete => "Del",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PgUp",
            Key.PageDown => "PgDn",
            Key.Left => "Left",
            Key.Up => "Up",
            Key.Right => "Right",
            Key.Down => "Down",
            _ => key.ToString()
        };
    }

    public static string FromVirtualKey(int vk)
    {
        if (vk is >= 0x41 and <= 0x5A)
            return ((char)vk).ToString();
        if (vk is >= 0x30 and <= 0x39)
            return ((char)vk).ToString();
        if (vk is >= 0x70 and <= 0x87)
            return "F" + (vk - 0x6F);
        if (vk is >= 0x60 and <= 0x69)
            return "Num " + (vk - 0x60);

        return vk switch
        {
            0x05 => "Mouse 4",
            0x06 => "Mouse 5",
            0xC0 => "`",
            0xBD => "-",
            0xBB => "=",
            0xDB => "[",
            0xDD => "]",
            0xDC => "\\",
            0xBA => ";",
            0xDE => "'",
            0xBC => ",",
            0xBE => ".",
            0xBF => "/",
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Enter",
            0x2D => "Ins",
            0x2E => "Del",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PgUp",
            0x22 => "PgDn",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            _ => "Key " + vk
        };
    }
}
