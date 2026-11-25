namespace DeskDot.Core;

/// <summary>
/// Pure countdown arithmetic for the focus card: formatting, clamping and percentage.
/// The timer that drives it (a DispatcherTimer) lives in <c>Services.FocusTimerService</c>.
/// </summary>
public static class FocusClock
{
    public const int MinMinutes = 1;
    public const int MaxMinutes = 180;

    public static TimeSpan ClampDuration(int minutes) =>
        TimeSpan.FromMinutes(Math.Clamp(minutes, MinMinutes, MaxMinutes));

    /// <summary>Formats a duration as <c>mm:ss</c> (clamped at zero, minutes capped at 99+).</summary>
    public static string Format(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "00:00";
        }

        var totalMinutes = (int)remaining.TotalMinutes;
        return string.Create(
            5,
            (totalMinutes, remaining.Seconds),
            static (span, state) =>
            {
                var (minutes, seconds) = state;
                if (minutes > 99)
                {
                    minutes = 99;
                }

                span[0] = (char)('0' + minutes / 10);
                span[1] = (char)('0' + minutes % 10);
                span[2] = ':';
                span[3] = (char)('0' + seconds / 10);
                span[4] = (char)('0' + seconds % 10);
            });
    }

    /// <summary>Fraction of the session still remaining, 0..100.</summary>
    public static double RemainingPercent(TimeSpan remaining, TimeSpan total)
    {
        if (total <= TimeSpan.Zero)
        {
            return 0;
        }

        var percent = remaining.TotalSeconds / total.TotalSeconds * 100;
        return Math.Clamp(percent, 0, 100);
    }
}
