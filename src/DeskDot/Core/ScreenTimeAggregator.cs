namespace DeskDot.Core;

/// <summary>
/// Pure aggregation for the "Screen time" card: turns raw foreground-window samples into
/// per-application totals, a daily headline figure and a comparison with yesterday.
/// </summary>
public static class ScreenTimeAggregator
{
    /// <summary>One stretch of time a given process owned the foreground.</summary>
    public readonly record struct Sample(string ProcessName, TimeSpan Duration);

    /// <summary>Totals for a single application.</summary>
    public readonly record struct AppTotal(string ProcessName, TimeSpan Duration, double Percent);

    /// <summary>
    /// Collapses samples into descending per-app totals, dropping sub-minute noise.
    /// Percentages are relative to the summed total, not to wall-clock time.
    /// </summary>
    public static IReadOnlyList<AppTotal> Summarise(IEnumerable<Sample> samples, TimeSpan minimum = default)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (minimum <= TimeSpan.Zero)
        {
            minimum = TimeSpan.FromMinutes(1);
        }

        var totals = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        foreach (var sample in samples)
        {
            if (sample.Duration <= TimeSpan.Zero || string.IsNullOrWhiteSpace(sample.ProcessName))
            {
                continue;
            }

            totals[sample.ProcessName] = totals.TryGetValue(sample.ProcessName, out var existing)
                ? existing + sample.Duration
                : sample.Duration;
        }

        var kept = totals.Where(pair => pair.Value >= minimum).ToList();
        if (kept.Count == 0)
        {
            // Everything was tiny: still report the largest entry rather than nothing.
            kept = totals.OrderByDescending(pair => pair.Value).Take(1).ToList();
        }

        var sum = kept.Sum(pair => pair.Value.TotalSeconds);
        return kept
            .OrderByDescending(pair => pair.Value)
            .Select(pair => new AppTotal(
                pair.Key,
                pair.Value,
                sum <= 0 ? 0 : Math.Round(pair.Value.TotalSeconds / sum * 100, 1)))
            .ToList();
    }

    /// <summary>Formats a duration the way the card reads it: <c>2h 34m</c>, <c>48m</c>, <c>45s</c>.</summary>
    public static string FormatDuration(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        var hours = (int)value.TotalHours;
        var minutes = value.Minutes;

        if (hours > 0)
        {
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        if (minutes > 0)
        {
            return $"{minutes}m";
        }

        return $"{Math.Max(0, value.Seconds)}s";
    }

    /// <summary>
    /// Change versus the previous day as a signed percentage of that day's total.
    /// Returns <c>null</c> when yesterday has no data to compare against.
    /// </summary>
    public static double? TrendPercent(TimeSpan today, TimeSpan yesterday)
    {
        if (yesterday <= TimeSpan.Zero)
        {
            return null;
        }

        return Math.Round((today.TotalSeconds - yesterday.TotalSeconds) / yesterday.TotalSeconds * 100, 1);
    }

    /// <summary>The gentle one-liner under the total.</summary>
    public static string Encouragement(TimeSpan today, TimeSpan yesterday)
    {
        var trend = TrendPercent(today, yesterday);

        if (trend is null)
        {
            return "A fresh start. Let's see how today goes.";
        }

        return trend switch
        {
            <= -20 => "A little less than yesterday. Nice.",
            < -5 => "Slightly less than yesterday. Good going.",
            <= 5 => "About the same as yesterday.",
            <= 25 => "A little more than yesterday. Take a breath.",
            _ => "Quite a bit more than yesterday. Time for a break?"
        };
    }

    /// <summary>
    /// Seven bars, oldest first, scaled to 0..1 for the week strip. Missing days read as 0.
    /// </summary>
    public static IReadOnlyList<double> WeekBars(IReadOnlyList<TimeSpan> days, int desiredCount = 7)
    {
        ArgumentNullException.ThrowIfNull(days);

        var slice = days.Count > desiredCount
            ? days.Skip(days.Count - desiredCount).ToList()
            : days.ToList();

        while (slice.Count < desiredCount)
        {
            slice.Insert(0, TimeSpan.Zero);
        }

        var peak = slice.Count == 0 ? 0 : slice.Max(day => day.TotalSeconds);
        return slice
            .Select(day => peak <= 0 ? 0 : Math.Clamp(day.TotalSeconds / peak, 0.06, 1))
            .ToList();
    }
}
