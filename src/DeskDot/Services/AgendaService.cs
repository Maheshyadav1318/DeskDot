using System.Globalization;
using System.IO;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Supplies the "Next · HH:mm" card. Reads %APPDATA%\DeskDot\agenda.txt when present
/// (one "HH:mm Title" entry per line) and otherwise falls back to a short demo agenda,
/// so the island is never empty on a fresh install.
/// </summary>
public sealed class AgendaService
{
    private static readonly AgendaItem[] DemoAgenda =
    {
        new("09:30", "Stand-up & inbox triage"),
        new("11:00", "Deep work block — DeskDot polish"),
        new("14:00", "Test sleep/resume & hotkey re-registration"),
        new("16:30", "Review pull requests"),
        new("18:00", "Ship notes & wrap up ✨")
    };

    private readonly string _agendaPath;

    public AgendaService()
    {
        _agendaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskDot",
            "agenda.txt");
    }

    public string AgendaPath => _agendaPath;

    public AgendaItem GetNext(DateTime now)
    {
        var items = LoadUserAgenda() ?? DemoAgenda.ToList();

        var timed = new List<(AgendaItem Item, TimeSpan Time)>();
        foreach (var item in items)
        {
            if (ParseTime(item.Time) is { } time)
            {
                timed.Add((item, time));
            }
        }

        if (timed.Count == 0)
        {
            return AgendaItem.None;
        }

        timed.Sort((a, b) => a.Time.CompareTo(b.Time));

        foreach (var entry in timed)
        {
            if (entry.Time >= now.TimeOfDay)
            {
                return entry.Item;
            }
        }

        // Past everything on the list: show tomorrow's first item.
        return timed[0].Item;
    }

    private List<AgendaItem>? LoadUserAgenda()
    {
        try
        {
            if (!File.Exists(_agendaPath))
            {
                return null;
            }

            var items = new List<AgendaItem>();
            foreach (var raw in File.ReadAllLines(_agendaPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var split = line.IndexOf(' ');
                if (split <= 0)
                {
                    continue;
                }

                var time = line[..split];
                var title = line[(split + 1)..].Trim();
                if (title.Length == 0)
                {
                    continue;
                }

                items.Add(new AgendaItem(time, title));
            }

            return items.Count > 0 ? items : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static TimeSpan? ParseTime(string text)
    {
        if (TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var shortTime) ||
            TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out shortTime))
        {
            return shortTime;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.TimeOfDay;
        }

        return null;
    }
}
