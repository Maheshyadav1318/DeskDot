using System.Text.Json.Serialization;

namespace DeskDot.Models;

/// <summary>User-tweakable behaviour persisted to %APPDATA%\DeskDot\settings.json.</summary>
public sealed class AppSettings
{
    /* ---------- Window ---------- */

    /// <summary>Global shortcut that shows / hides the dashboard.</summary>
    public string ToggleHotkey { get; set; } = "Ctrl+Alt+D";

    /// <summary>Shortcut that opens DeskDot Setup.</summary>
    public string SettingsHotkey { get; set; } = "Ctrl+OemComma";

    /// <summary>Keep the dashboard above other windows.</summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>Start DeskDot minimised to the tray.</summary>
    public bool StartMinimised { get; set; }

    /// <summary>Launch DeskDot when the user signs in.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Remembers window size and position.</summary>
    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    /// <summary>Whether the window was maximised when last closed.</summary>
    public bool WindowMaximised { get; set; }

    /* ---------- Personalisation ---------- */

    /// <summary>Windows account name shown in the greeting / hero.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Length of the focus session, in minutes.</summary>
    public int FocusMinutes { get; set; } = 25;

    /// <summary>How often system stats are sampled, in milliseconds.</summary>
    public int StatsIntervalMs { get; set; } = 3000;

    /* ---------- Modules ---------- */

    /// <summary>Enabled module identifiers; missing modules fall back to the defaults.</summary>
    public List<string> EnabledModules { get; set; } = new();

    /* ---------- Weather ---------- */

    /// <summary>Fetch live weather from Open-Meteo. Off means the card shows a placeholder.</summary>
    public bool WeatherEnabled { get; set; } = true;

    /// <summary>City shown on the weather card, e.g. "Portland, OR".</summary>
    public string WeatherLocation { get; set; } = "Portland, OR";

    /// <summary>Resolved latitude for the weather request.</summary>
    public double? WeatherLatitude { get; set; } = 45.5231;

    /// <summary>Resolved longitude for the weather request.</summary>
    public double? WeatherLongitude { get; set; } = -122.6765;

    /// <summary>Use °F instead of °C.</summary>
    public bool WeatherUseFahrenheit { get; set; }

    /* ---------- Privacy-sensitive modules ---------- */

    /// <summary>Record clipboard history (text only, local, capped). Off by default.</summary>
    public bool ClipboardHistoryEnabled { get; set; }

    /// <summary>Track which applications you use (local totals only). Off by default.</summary>
    public bool ScreenTimeEnabled { get; set; }

    /// <summary>Mask clipboard previews that look like secrets.</summary>
    public bool MaskSensitiveClipboard { get; set; } = true;

    [JsonIgnore]
    public double SessionLengthSeconds => Math.Max(1, FocusMinutes) * 60.0;

    public AppSettings Clone()
    {
        var copy = (AppSettings)MemberwiseClone();
        copy.EnabledModules = new List<string>(EnabledModules);
        return copy;
    }
}
