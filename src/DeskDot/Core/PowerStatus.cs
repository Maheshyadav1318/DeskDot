namespace DeskDot.Core;

/// <summary>Pure formatting for the battery read-out.</summary>
public static class PowerStatus
{
    /// <summary>
    /// Describes the battery the way the "This PC · Live" card shows it.
    /// <paramref name="percent"/> is 0..100 and may be negative when no battery exists.
    /// </summary>
    public static string Describe(int percent, bool isCharging, bool hasBattery = true)
    {
        if (!hasBattery || percent < 0)
        {
            return "No battery";
        }

        var clamped = Math.Clamp(percent, 0, 100);

        if (isCharging && clamped >= 100)
        {
            return "100% Charging";
        }

        if (isCharging)
        {
            return $"{clamped}% Charging";
        }

        return $"{clamped}%";
    }

    /// <summary>Short state word used as a label ("Charging", "On battery", "Plugged in").</summary>
    public static string StateLabel(int percent, bool isCharging, bool hasBattery = true)
    {
        if (!hasBattery || percent < 0)
        {
            return "Desktop";
        }

        return isCharging ? "Charging" : "On battery";
    }

    /// <summary>True when the machine should show a low-battery warning.</summary>
    public static bool IsLow(int percent, bool isCharging, bool hasBattery = true, int threshold = 20) =>
        hasBattery && percent >= 0 && !isCharging && percent <= threshold;
}
