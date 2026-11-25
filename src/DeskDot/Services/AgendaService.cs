using System.IO;
using DeskDot.Core;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Supplies the "Next · HH:mm" card. Reads <c>%APPDATA%\DeskDot\agenda.txt</c> when present
/// and otherwise falls back to the bundled demo agenda, so the island is never empty.
/// Parsing and selection live in <see cref="AgendaPlanner"/> (pure and unit tested).
/// </summary>
public sealed class AgendaService
{
    private readonly string _agendaPath;

    public AgendaService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskDot",
            "agenda.txt"))
    {
    }

    /// <summary>Test seam: point the service at any file path.</summary>
    public AgendaService(string agendaPath)
    {
        _agendaPath = agendaPath ?? throw new ArgumentNullException(nameof(agendaPath));
    }

    public string AgendaPath => _agendaPath;

    public AgendaItem GetNext(DateTime now) => GetNext(TimeOnly.FromDateTime(now));

    public AgendaItem GetNext(TimeOnly now)
    {
        var userAgenda = TryLoadUserAgenda();
        return AgendaPlanner.Next(userAgenda ?? AgendaPlanner.DemoAgenda, now);
    }

    private IReadOnlyList<AgendaItem>? TryLoadUserAgenda()
    {
        try
        {
            if (!File.Exists(_agendaPath))
            {
                return null;
            }

            var items = AgendaPlanner.Parse(File.ReadLines(_agendaPath));
            return items.Count > 0 ? items : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // unreadable agenda: fall back to the demo list
        }
    }
}
