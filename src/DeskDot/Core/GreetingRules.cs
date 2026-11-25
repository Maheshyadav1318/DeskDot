namespace DeskDot.Core;

/// <summary>Pure greeting/time formatting rules used by the island header.</summary>
public static class GreetingRules
{
    /// <summary>
    /// Time-of-day greeting. <paramref name="displayName"/> is optional and is folded into
    /// the sentence when present.
    /// </summary>
    public static string ForHour(int hour, string? displayName = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? string.Empty : $", {displayName.Trim()}";

        return hour switch
        {
            < 0 or > 23 => $"Have a wonderful day{name}! ✨",
            < 5 => $"Still up{name}? Take it easy. 🌙",
            < 12 => $"Good morning{name}! ✨",
            < 17 => $"Have a wonderful day{name}! ✨",
            < 21 => $"Good evening{name}! 🌇",
            _ => $"Winding down{name}. 🌙"
        };
    }
}
