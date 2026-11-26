namespace DeskDot.Models;

/// <summary>The eight modules advertised by the dashboard.</summary>
public enum DashboardModule
{
    Focus,
    Weather,
    System,
    ScreenTime,
    Clock,
    Clipboard,
    Notes,
    Agenda
}

/// <summary>Metadata for one module row in the module manager.</summary>
public sealed record ModuleInfo(DashboardModule Id, string Name, string Description, bool IsPrivate = false)
{
    public string Key => Id.ToString();

    public static IReadOnlyList<ModuleInfo> All { get; } = new[]
    {
        new ModuleInfo(DashboardModule.Focus, "Focus", "A 25-minute timer with a quiet chime"),
        new ModuleInfo(DashboardModule.Weather, "Weather", "Live conditions — your coordinates go to Open-Meteo"),
        new ModuleInfo(DashboardModule.System, "System", "CPU, memory and battery, live from this PC"),
        new ModuleInfo(DashboardModule.ScreenTime, "Screen time", "Where the hours went today — off until you opt in", IsPrivate: true),
        new ModuleInfo(DashboardModule.Clock, "Clock", "Time, date and a gentle greeting"),
        new ModuleInfo(DashboardModule.Clipboard, "Clipboard", "Text you copy, held in memory on this device", IsPrivate: true),
        new ModuleInfo(DashboardModule.Notes, "Notes", "Quick notes saved locally to your PC"),
        new ModuleInfo(DashboardModule.Agenda, "Agenda", "Up next, read from your agenda file")
    };

    /// <summary>Modules that work out of the box; the two private ones must be opted into.</summary>
    public static IReadOnlyList<DashboardModule> Defaults { get; } = new[]
    {
        DashboardModule.Focus,
        DashboardModule.Weather,
        DashboardModule.System,
        DashboardModule.Clock,
        DashboardModule.Notes,
        DashboardModule.Agenda
    };
}

/// <summary>Current weather for the card.</summary>
public sealed record WeatherSnapshot(
    string Location,
    double TemperatureC,
    double HighC,
    double LowC,
    int Code,
    int HumidityPercent,
    double WindKph,
    double WindDirectionDegrees,
    DateTimeOffset UpdatedAt)
{
    public static readonly WeatherSnapshot Empty = new(
        "—", 0, 0, 0, 3, 0, 0, 0, DateTimeOffset.MinValue);

    public bool HasData => UpdatedAt != DateTimeOffset.MinValue;

    public double ToDisplay(double celsius, bool fahrenheit) => fahrenheit ? celsius * 9 / 5 + 32 : celsius;
}

/// <summary>One clipboard history row.</summary>
public sealed record ClipboardEntry(string Text, DateTimeOffset CapturedAt, bool IsSensitive)
{
    public string Preview => Text;

    public static ClipboardEntry FromText(string text, DateTimeOffset capturedAt, bool sensitive) =>
        new(text, capturedAt, sensitive);
}

/// <summary>A local quick note.</summary>
public sealed record NoteEntry(Guid Id, string Title, string Body, DateTimeOffset UpdatedAt)
{
    public static NoteEntry Create(string title, string body) =>
        new(Guid.NewGuid(), title, body, DateTimeOffset.Now);

    public string Subtitle => string.IsNullOrWhiteSpace(Body) ? "Empty note" : Body;
}

/// <summary>One application's share of today's screen time.</summary>
public sealed record AppUsage(string ProcessName, string DisplayName, TimeSpan Duration, double Percent)
{
    /// <summary>Preformatted for binding ("1h 12m"); TimeSpan.StringFormat is culture-fragile.</summary>
    public string DurationText => Core.ScreenTimeAggregator.FormatDuration(Duration);
}
