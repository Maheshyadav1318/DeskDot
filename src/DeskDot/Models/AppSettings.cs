using System.Text.Json.Serialization;

namespace DeskDot.Models;

/// <summary>User-tweakable behaviour persisted to %APPDATA%\DeskDot\settings.json.</summary>
public sealed class AppSettings
{
    /// <summary>Global shortcut that expands / collapses the island.</summary>
    public string ToggleHotkey { get; set; } = "Alt+Space";

    /// <summary>Collapse the island when it loses focus.</summary>
    public bool CollapseOnFocusLoss { get; set; } = true;

    /// <summary>Keep the island above other windows.</summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>Launch DeskDot when the user signs in.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>How often system stats are sampled, in milliseconds.</summary>
    public int StatsIntervalMs { get; set; } = 4000;

    /// <summary>Length of the focus session, in minutes.</summary>
    public int FocusMinutes { get; set; } = 25;

    /// <summary>Windows account name shown in the greeting.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Last island position in device independent pixels (top-left), or <c>null</c> when the
    /// user has not moved it yet (the island then centres itself on the primary monitor).
    /// Nullable rather than NaN because NaN is not valid JSON.
    /// </summary>
    public double? IslandLeft { get; set; }

    public double? IslandTop { get; set; }

    [JsonIgnore]
    public double SessionLengthSeconds => Math.Max(1, FocusMinutes) * 60.0;

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
