using System.IO;
using DeskDot.Core;

namespace DeskDot.Services;

/// <summary>
/// Supplies the "Up next" card from <c>%APPDATA%\DeskDot\agenda.txt</c>. Parsing and the
/// "what's next" decision live in <see cref="DayPlan"/> (pure and unit tested); this class
/// only does file IO. An empty result means the user has not written an agenda yet, and the
/// dashboard says so instead of inventing entries.
/// </summary>
public sealed class AgendaService
{
    private readonly string _agendaPath;

    public AgendaService()
        : this(DefaultPath)
    {
    }

    /// <summary>Test seam: point the service at any file path.</summary>
    public AgendaService(string agendaPath)
    {
        _agendaPath = agendaPath ?? throw new ArgumentNullException(nameof(agendaPath));
    }

    public string AgendaPath => _agendaPath;

    /// <summary>Where the agenda lives, for the empty-state hint.</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskDot", "agenda.txt");

    /// <summary>The whole plan for today, or an empty list when there is no usable file.</summary>
    public IReadOnlyList<PlanEntry> GetPlan()
    {
        try
        {
            if (!File.Exists(_agendaPath))
            {
                return Array.Empty<PlanEntry>();
            }

            return DayPlan.Parse(File.ReadLines(_agendaPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<PlanEntry>();
        }
    }
}
