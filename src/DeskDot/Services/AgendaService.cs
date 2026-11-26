using System.IO;
using DeskDot.Core;

namespace DeskDot.Services;

/// <summary>
/// Supplies the "Up next" card from <c>%APPDATA%\DeskDot\agenda.txt</c>. Parsing and the
/// "what's next" decision live in <see cref="DayPlan"/> (pure and unit tested); this class
/// only does file IO. An empty result means the caller should use its own sample plan.
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
