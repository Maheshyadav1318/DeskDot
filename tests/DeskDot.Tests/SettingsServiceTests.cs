using System.Text.Json;
using DeskDot.Models;
using DeskDot.Services;
using Xunit;

namespace DeskDot.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "deskdot-settings-" + Guid.NewGuid().ToString("N"));

    public SettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // best effort
        }
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_missing()
    {
        var service = new SettingsService(_dir);

        var settings = service.Load();

        Assert.Equal("Ctrl+Alt+D", settings.ToggleHotkey);
        Assert.Equal("Ctrl+OemComma", settings.SettingsHotkey);
        Assert.Equal(25, settings.FocusMinutes);
        Assert.False(settings.ClipboardHistoryEnabled);
        Assert.False(settings.ScreenTimeEnabled);
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var service = new SettingsService(_dir);
        var settings = new AppSettings
        {
            ToggleHotkey = "Ctrl+Shift+D",
            SettingsHotkey = "Ctrl+Alt+S",
            AlwaysOnTop = false,
            StartWithWindows = true,
            StatsIntervalMs = 2000,
            FocusMinutes = 45,
            DisplayName = "Mahesh"
        };

        Assert.True(service.Save(settings));
        Assert.True(File.Exists(Path.Combine(_dir, "settings.json")));

        var loaded = service.Load();

        Assert.Equal("Ctrl+Shift+D", loaded.ToggleHotkey);
        Assert.Equal("Ctrl+Alt+S", loaded.SettingsHotkey);
        Assert.False(loaded.AlwaysOnTop);
        Assert.True(loaded.StartWithWindows);
        Assert.Equal(2000, loaded.StatsIntervalMs);
        Assert.Equal(45, loaded.FocusMinutes);
        Assert.Equal("Mahesh", loaded.DisplayName);
    }

    [Fact]
    public void Save_leaves_no_temp_file_behind()
    {
        var service = new SettingsService(_dir);

        service.Save(new AppSettings());

        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void Load_recovers_from_corrupt_json()
    {
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "{ this is not json ");
        var service = new SettingsService(_dir);

        var settings = service.Load();

        Assert.Equal("Ctrl+Alt+D", settings.ToggleHotkey);
    }

    [Fact]
    public void Load_recovers_from_empty_file()
    {
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "   ");
        var service = new SettingsService(_dir);

        Assert.Equal(25, service.Load().FocusMinutes);
    }

    [Fact]
    public void Load_applies_defaults_for_missing_properties()
    {
        File.WriteAllText(Path.Combine(_dir, "settings.json"), """{ "DisplayName": "Only this" }""");
        var service = new SettingsService(_dir);

        var settings = service.Load();

        Assert.Equal("Only this", settings.DisplayName);
        Assert.Equal("Ctrl+Alt+D", settings.ToggleHotkey);
        Assert.Equal(3000, settings.StatsIntervalMs);
    }

    [Fact]
    public void Save_rejects_null_settings()
    {
        var service = new SettingsService(_dir);

        Assert.Throws<ArgumentNullException>(() => service.Save(null!));
    }

    [Fact]
    public void SessionLengthSeconds_tracks_focus_minutes()
    {
        var settings = new AppSettings { FocusMinutes = 30 };

        Assert.Equal(1800, settings.SessionLengthSeconds, 3);
    }

    [Fact]
    public void Default_settings_serialize_without_throwing()
    {
        // Regression: the window position used to default to double.NaN, which
        // System.Text.Json refuses to write — saving settings failed and nothing persisted.
        var json = JsonSerializer.Serialize(new AppSettings(), SettingsService.SerializerOptions);

        Assert.DoesNotContain("NaN", json);
        Assert.DoesNotContain("Infinity", json);
    }

    [Fact]
    public void Position_round_trips_and_defaults_to_null()
    {
        var service = new SettingsService(_dir);

        Assert.Null(service.Load().WindowLeft);

        Assert.True(service.Save(new AppSettings
        {
            WindowLeft = 120.5,
            WindowTop = 40,
            WindowWidth = 1160,
            WindowHeight = 880
        }));
        var loaded = service.Load();

        Assert.Equal(120.5, loaded.WindowLeft);
        Assert.Equal(40, loaded.WindowTop);
        Assert.Equal(1160, loaded.WindowWidth);
        Assert.Equal(880, loaded.WindowHeight);
    }

    [Fact]
    public void Module_keys_are_stable()
    {
        // These strings are persisted in settings.json. Renaming one would silently reset a
        // user's module choices, so the set is pinned here on purpose.
        var keys = ModuleInfo.All.Select(module => module.Key).ToArray();

        Assert.Equal(
            new[] { "Focus", "Weather", "System", "ScreenTime", "Clock", "Clipboard", "Notes", "Agenda" },
            keys);
        Assert.Equal(8, keys.Length);
        Assert.Equal(keys.Length, keys.Distinct().Count());
    }

    [Fact]
    public void Default_modules_exclude_the_private_ones()
    {
        Assert.DoesNotContain(DashboardModule.Clipboard, ModuleInfo.Defaults);
        Assert.DoesNotContain(DashboardModule.ScreenTime, ModuleInfo.Defaults);
        Assert.Contains(DashboardModule.Focus, ModuleInfo.Defaults);
    }

    [Fact]
    public void Privacy_sensitive_modules_are_off_by_default()
    {
        var settings = new AppSettings();

        Assert.False(settings.ClipboardHistoryEnabled);
        Assert.False(settings.ScreenTimeEnabled);
        Assert.True(settings.MaskSensitiveClipboard);
    }

    [Fact]
    public void Module_list_round_trips()
    {
        var service = new SettingsService(_dir);
        var settings = new AppSettings { EnabledModules = new List<string> { "System", "Clock", "Notes" } };

        Assert.True(service.Save(settings));

        Assert.Equal(new[] { "System", "Clock", "Notes" }, service.Load().EnabledModules);
    }

    [Fact]
    public void Weather_location_round_trips()
    {
        var service = new SettingsService(_dir);
        var settings = new AppSettings
        {
            WeatherLocation = "Guntur, Andhra Pradesh, India",
            WeatherLatitude = 16.3067,
            WeatherLongitude = 80.4365,
            WeatherUseFahrenheit = true
        };

        Assert.True(service.Save(settings));
        var loaded = service.Load();

        Assert.Equal("Guntur, Andhra Pradesh, India", loaded.WeatherLocation);
        Assert.Equal(16.3067, loaded.WeatherLatitude!.Value, 4);
        Assert.Equal(80.4365, loaded.WeatherLongitude!.Value, 4);
        Assert.True(loaded.WeatherUseFahrenheit);
    }

    [Fact]
    public void Clone_does_not_share_the_module_list()
    {
        var original = new AppSettings { EnabledModules = new List<string> { "System" } };

        var copy = original.Clone();
        copy.EnabledModules.Add("Notes");

        Assert.Single(original.EnabledModules);
    }

    [Fact]
    public void Serializer_options_are_stable()
    {
        // Guards against accidentally enabling a naming policy that would break
        // existing %APPDATA%\DeskDot\settings.json files.
        var json = JsonSerializer.Serialize(new AppSettings(), SettingsService.SerializerOptions);

        Assert.Contains("\"ToggleHotkey\"", json);
        Assert.Contains("\"FocusMinutes\"", json);
    }
}
