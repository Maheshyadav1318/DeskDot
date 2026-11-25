using System.Globalization;
using DeskDot.Models;

namespace DeskDot.Core;

/// <summary>
/// Pure agenda logic: parsing <c>agenda.txt</c> lines and choosing the next item.
/// Kept free of file and UI dependencies so it can be unit tested on any platform.
/// </summary>
public static class AgendaPlanner
{
    /// <summary>Fallback agenda used when the user has not written an agenda file.</summary>
    public static IReadOnlyList<AgendaItem> DemoAgenda { get; } = new[]
    {
        new AgendaItem("09:30", "Stand-up & inbox triage"),
        new AgendaItem("11:00", "Deep work block — DeskDot polish"),
        new AgendaItem("14:00", "Test sleep/resume & hotkey re-registration"),
        new AgendaItem("16:30", "Review pull requests"),
        new AgendaItem("18:00", "Ship notes & wrap up ✨")
    };

    /// <summary>
    /// Parses agenda lines of the form <c>HH:mm Title</c>. Blank lines and lines starting
    /// with <c>#</c> are ignored; unparseable lines are skipped rather than throwing.
    /// </summary>
    public static List<AgendaItem> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var items = new List<AgendaItem>();
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
            if (split <= 0)
            {
                continue;
            }

            var title = line[(split + 1)..].Trim();
            if (title.Length == 0 || ParseTime(line[..split]) is null)
            {
                continue;
            }

            items.Add(new AgendaItem(line[..split], title));
        }

        return items;
    }

    /// <summary>
    /// Picks the next agenda item at or after <paramref name="now"/>. When the whole list is
    /// in the past the first item of the day is returned (it becomes "tomorrow's" first).
    /// </summary>
    public static AgendaItem Next(IReadOnlyList<AgendaItem> items, TimeOnly now)
    {
        if (items is null || items.Count == 0)
        {
            return AgendaItem.None;
        }

        AgendaItem? best = null;
        var bestTime = TimeSpan.MaxValue;
        var first = items[0];
        var firstTime = TimeSpan.MaxValue;

        foreach (var item in items)
        {
            if (ParseTime(item.Time) is not { } time)
            {
                continue;
            }

            if (time < firstTime)
            {
                firstTime = time;
                first = item;
            }

            if (time >= now.ToTimeSpan() && time < bestTime)
            {
                bestTime = time;
                best = item;
            }
        }

        return best ?? (firstTime == TimeSpan.MaxValue ? AgendaItem.None : first);
    }

    /// <summary>Accepts <c>HH:mm</c>, <c>H:mm</c> and full date-time strings.</summary>
    public static TimeSpan? ParseTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();

        if (TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var exact) ||
            TimeSpan.TryParseExact(text, @"h\:mm", CultureInfo.InvariantCulture, out exact))
        {
            return exact;
        }

        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var parsed) &&
            parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1))
        {
            return parsed;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime.TimeOfDay;
        }

        return null;
    }
}
