using System.Globalization;

namespace DeskDot.Core;

/// <summary>An agenda entry as the dashboard shows it ("11:30AM", "Design catch-up", "30 min · Product design").</summary>
public sealed record PlanEntry(string Time, string Title, string? Detail = null)
{
    public static readonly PlanEntry None = new(string.Empty, "Nothing planned", null);
}

/// <summary>
/// Pure agenda logic: parsing <c>agenda.txt</c> lines (<c>HH:mm Title [| detail]</c>) and
/// picking what is "up next" for a given moment.
/// </summary>
public static class DayPlan
{
    /// <summary>
    /// Parses lines of the form <c>HH:mm Title</c> or <c>HH:mm Title | detail</c>.
    /// Blank lines and <c>#</c> comments are ignored; invalid times are skipped.
    /// </summary>
    public static List<PlanEntry> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var entries = new List<PlanEntry>();
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var line = raw.Trim();
            if (line.StartsWith('#'))
            {
                continue;
            }

            var split = line.IndexOf(' ', StringComparison.Ordinal);
            if (split <= 0 || ParseClock(line[..split]) is null)
            {
                continue;
            }

            var rest = line[(split + 1)..].Trim();
            if (rest.Length == 0)
            {
                continue;
            }

            string? detail = null;
            var bar = rest.IndexOf('|', StringComparison.Ordinal);
            if (bar >= 0)
            {
                detail = rest[(bar + 1)..].Trim();
                rest = rest[..bar].Trim();
                if (detail.Length == 0)
                {
                    detail = null;
                }
            }

            if (rest.Length == 0)
            {
                continue;
            }

            entries.Add(new PlanEntry(Normalise(line[..split]), rest, detail));
        }

        return entries;
    }

    /// <summary>
    /// Splits the day into what is still ahead and what has passed, both ordered by time.
    /// </summary>
    public static (IReadOnlyList<PlanEntry> Upcoming, IReadOnlyList<PlanEntry> Earlier) Split(
        IReadOnlyList<PlanEntry> entries,
        TimeOnly now)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var timed = entries
            .Select(entry => (Entry: entry, Time: ParseClock(entry.Time)))
            .Where(pair => pair.Time is not null)
            .OrderBy(pair => pair.Time!.Value)
            .ToList();

        var upcoming = new List<PlanEntry>();
        var earlier = new List<PlanEntry>();

        foreach (var pair in timed)
        {
            if (pair.Time!.Value >= now.ToTimeSpan())
            {
                upcoming.Add(pair.Entry);
            }
            else
            {
                earlier.Add(pair.Entry);
            }
        }

        return (upcoming, earlier);
    }

    /// <summary>The next entry at or after <paramref name="now"/>, or <c>None</c>.</summary>
    public static PlanEntry Next(IReadOnlyList<PlanEntry> entries, TimeOnly now)
    {
        var (upcoming, _) = Split(entries, now);
        return upcoming.Count > 0 ? upcoming[0] : PlanEntry.None;
    }

    /// <summary>Counts entries still ahead today, used for the "N ahead" label.</summary>
    public static int RemainingToday(IReadOnlyList<PlanEntry> entries, TimeOnly now) =>
        Split(entries, now).Upcoming.Count;

    /// <summary>Formats a time as the card shows it: <c>11:30AM</c>.</summary>
    public static string FormatClock(string time) =>
        ParseClock(time) is { } parsed ? FormatClock(parsed) : time;

    public static string FormatClock(TimeSpan time)
    {
        var hours24 = (int)time.TotalHours % 24;
        var minutes = time.Minutes;
        var suffix = hours24 < 12 ? "AM" : "PM";
        var hours12 = hours24 % 12;
        if (hours12 == 0)
        {
            hours12 = 12;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{hours12:00}:{minutes:00}{suffix}");
    }

    public static string FormatClock(TimeOnly time) => FormatClock(time.ToTimeSpan());

    /// <summary>Parses <c>HH:mm</c>, <c>H:mm</c> or <c>h:mm AM/PM</c>.</summary>
    public static TimeSpan? ParseClock(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();

        // A bare "11:30" must stay 24-hour even when the current locale is 12-hour, so the
        // numeric forms are parsed with an invariant culture first.
        if (TimeSpan.TryParseExact(trimmed, @"hh\:mm", CultureInfo.InvariantCulture, out var exact) ||
            TimeSpan.TryParseExact(trimmed, @"h\:mm", CultureInfo.InvariantCulture, out exact))
        {
            return Validate(exact);
        }

        if (trimmed.EndsWith("AM", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("PM", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = trimmed[^2..].ToUpperInvariant();
            var body = trimmed[..^2].Trim();

            if (TimeSpan.TryParseExact(body, @"hh\:mm", CultureInfo.InvariantCulture, out var twelve) ||
                TimeSpan.TryParseExact(body, @"h\:mm", CultureInfo.InvariantCulture, out twelve))
            {
                var hours = twelve.Hours % 12;
                if (suffix == "PM")
                {
                    hours += 12;
                }

                return new TimeSpan(hours, twelve.Minutes, 0);
            }
        }

        // Numeric fallback: "11:30" or "9:5". Parsed digit-by-digit so a 12-hour host
        // locale can never reinterpret it as 23:30.
        var parts = trimmed.Split(':');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var rawHours) &&
            int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var rawMinutes) &&
            rawHours is >= 0 and <= 23 && rawMinutes is >= 0 and <= 59)
        {
            return new TimeSpan(rawHours, rawMinutes, 0);
        }

        return null;
    }

    private static TimeSpan? Validate(TimeSpan value) =>
        value >= TimeSpan.Zero && value < TimeSpan.FromDays(1) ? value : null;

    private static string Normalise(string time) =>
        ParseClock(time) is { } parsed ? FormatClock24(parsed) : time;

    private static string FormatClock24(TimeSpan time) =>
        string.Create(CultureInfo.InvariantCulture, $"{(int)time.TotalHours:00}:{time.Minutes:00}");
}
