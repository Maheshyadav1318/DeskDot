namespace DeskDot.Core;

/// <summary>
/// Pure parsing/formatting for global hotkey gestures such as <c>Alt+Space</c>.
/// The Win32 registration itself lives in <c>Services.HotkeyService</c>; keeping the
/// string handling here makes it unit testable without a window handle.
/// </summary>
public static class HotkeyGesture
{
    public const string Default = "Alt+Space";

    /// <summary>Modifier bits matching the Win32 MOD_* constants.</summary>
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    /// <summary>
    /// Parses a gesture into modifier bits and a virtual-key code.
    /// Requires at least one modifier plus exactly one non-modifier key.
    /// </summary>
    public static bool TryParse(string? gesture, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(gesture))
        {
            return false;
        }

        var tokens = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        foreach (var token in tokens)
        {
            switch (token.ToLowerInvariant())
            {
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;
                default:
                    if (virtualKey != 0 || !TryParseKey(token, out var key))
                    {
                        return false; // two keys, or an unknown token
                    }

                    virtualKey = key;
                    break;
            }
        }

        return modifiers != 0 && virtualKey != 0;
    }

    /// <summary>
    /// Maps a key name to a virtual-key code using the Win32 letter/digit ranges and the
    /// function-key block, so no WPF dependency is needed.
    /// </summary>
    private static bool TryParseKey(string token, out uint virtualKey)
    {
        virtualKey = 0;

        if (token.Length == 1)
        {
            var c = char.ToUpperInvariant(token[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = c; // VK_A..VK_Z == 'A'..'Z', VK_0..VK_9 == '0'..'9'
                return true;
            }

            return false;
        }

        switch (token.ToLowerInvariant())
        {
            case "space":
                virtualKey = 0x20;
                return true;
            case "tab":
                virtualKey = 0x09;
                return true;
            case "enter":
            case "return":
                virtualKey = 0x0D;
                return true;
            case "escape":
            case "esc":
                virtualKey = 0x1B;
                return true;
            case "left":
                virtualKey = 0x25;
                return true;
            case "up":
                virtualKey = 0x26;
                return true;
            case "right":
                virtualKey = 0x27;
                return true;
            case "down":
                virtualKey = 0x28;
                return true;
        }

        if (token.Length is 2 or 3 && (token[0] is 'F' or 'f') &&
            int.TryParse(token.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + fn - 1); // VK_F1 == 0x70
            return true;
        }

        return false;
    }

    /// <summary>Formats modifier bits and a virtual-key code back into canonical text.</summary>
    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>(4);
        if ((modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((modifiers & ModShift) != 0) parts.Add("Shift");
        if ((modifiers & ModWin) != 0) parts.Add("Win");
        parts.Add(FormatKey(virtualKey));
        return string.Join('+', parts);
    }

    private static string FormatKey(uint virtualKey) => virtualKey switch
    {
        0x20 => "Space",
        0x09 => "Tab",
        0x0D => "Enter",
        0x1B => "Esc",
        0x25 => "Left",
        0x26 => "Up",
        0x27 => "Right",
        0x28 => "Down",
        >= 0x70 and <= 0x87 => $"F{virtualKey - 0x70 + 1}",
        >= 'A' and <= 'Z' => ((char)virtualKey).ToString(),
        >= '0' and <= '9' => ((char)virtualKey).ToString(),
        _ => $"VK{virtualKey:X2}"
    };
}
