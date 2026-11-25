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

        Assert.Equal("Alt+Space", settings.ToggleHotkey);
        Assert.Equal(25, settings.FocusMinutes);
        Assert.True(settings.CollapseOnFocusLoss);
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var service = new SettingsService(_dir);
        var settings = new AppSettings
        {
            ToggleHotkey = "Ctrl+Shift+D",
            CollapseOnFocusLoss = false,
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
        Assert.False(loaded.CollapseOnFocusLoss);
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

        Assert.Equal("Alt+Space", settings.ToggleHotkey);
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
        Assert.Equal("Alt+Space", settings.ToggleHotkey);
        Assert.Equal(4000, settings.StatsIntervalMs);
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
    public void Serializer_options_are_stable()
    {
        // Guards against accidentally enabling a naming policy that would break
        // existing %APPDATA%\DeskDot\settings.json files.
        var json = JsonSerializer.Serialize(new AppSettings(), SettingsService.SerializerOptions);

        Assert.Contains("\"ToggleHotkey\"", json);
        Assert.Contains("\"FocusMinutes\"", json);
    }
}
