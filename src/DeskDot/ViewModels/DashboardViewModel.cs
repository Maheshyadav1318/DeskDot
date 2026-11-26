using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DeskDot.Core;
using DeskDot.Models;
using DeskDot.Services;

namespace DeskDot.ViewModels;

/// <summary>
/// State for the dashboard. Owns the clock, system/battery sampling, focus timer, weather,
/// clipboard, screen time, notes and the agenda, and exposes everything the view binds to.
/// </summary>
public sealed class DashboardViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;

    private readonly SystemStatsService _stats = new();
    private readonly BatteryService _battery = new();
    private readonly FocusTimerService _focus;
    private readonly WeatherService _weather;
    private readonly ClipboardService _clipboard;
    private readonly ScreenTimeService _screenTime;
    private readonly NotesService _notes;
    private readonly AgendaService _agenda;

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _statsTimer;

    private AppSettings _settings;

    private SystemSnapshot _snapshot = SystemSnapshot.Empty;
    private BatteryReading _power = BatteryReading.Unknown;
    private FocusState _focusState;
    private WeatherSnapshot _weatherSnapshot = WeatherSnapshot.Empty;
    private string? _weatherError;
    private string _clipboardPreview = string.Empty;
    private string _clipboardAge = string.Empty;
    private string _quote;
    private string _quoteSource = string.Empty;
    private bool _disposed;

    public DashboardViewModel(AppSettings settings, AgendaService agenda, NotesService notes)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _agenda = agenda ?? throw new ArgumentNullException(nameof(agenda));
        _notes = notes ?? throw new ArgumentNullException(nameof(notes));

        _focus = new FocusTimerService(settings.FocusMinutes);
        _focusState = _focus.State;
        _quote = Quotes.First().Text;
        _quoteSource = Quotes.First().Source;

        _weather = new WeatherService(
            () => _settings.WeatherEnabled,
            () => (
                _settings.WeatherLatitude ?? 45.5231,
                _settings.WeatherLongitude ?? -122.6765,
                _settings.WeatherUseFahrenheit));

        _clipboard = new ClipboardService(
            () => _settings.ClipboardHistoryEnabled,
            () => _settings.MaskSensitiveClipboard);

        _screenTime = new ScreenTimeService(() => _settings.ScreenTimeEnabled);

        _focus.Changed += (_, state) =>
        {
            _focusState = state;
            Raise(nameof(FocusText));
            Raise(nameof(FocusActionLabel));
            Raise(nameof(FocusPercent));
        };

        _focus.Completed += (_, _) => FocusCompleted?.Invoke(this, EventArgs.Empty);

        _weather.Updated += (_, snapshot) =>
        {
            _weatherSnapshot = snapshot;
            _weatherError = _weather.LastError;
            RaiseWeather();
        };

        _clipboard.Changed += (_, _) => RaiseClipboard();
        _screenTime.Changed += (_, _) => RaiseScreenTime();
        _notes.Changed += (_, _) => RaiseNotes();

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => RaiseClock();

        _statsTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = StatsInterval(settings) };
        _statsTimer.Tick += (_, _) => RefreshSystem();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when a focus session hits zero so the shell can notify.</summary>
    public event EventHandler? FocusCompleted;

    public ObservableCollection<ModuleInfo> Modules { get; } = new();

    public ObservableCollection<AppUsage> TopApps { get; } = new();

    public ObservableCollection<PlanEntry> UpNext { get; } = new();

    public ObservableCollection<NoteEntry> RecentNotes { get; } = new();

    public IReadOnlyList<(string Text, string Source)> Quotes { get; } = new[]
    {
        ("You don't have to do it all.\nJust the next right thing.", "A little reminder"),
        ("Make room for what matters.", "A little reminder"),
        ("Slow is smooth. Smooth is fast.", "A little reminder"),
        ("One thing at a time is still progress.", "A little reminder"),
        ("Leave a little space in the day.", "A little reminder")
    };

    public void Start()
    {
        RefreshSystem();
        RefreshAgenda();
        RefreshNotes();
        RaiseClipboard();
        RaiseScreenTime();
        RaiseClock();
        RaiseWeather();

        RebuildModules();

        _clockTimer.Start();
        _statsTimer.Start();

        if (_settings.ClipboardHistoryEnabled)
        {
            _clipboard.Start();
        }

        if (_settings.ScreenTimeEnabled)
        {
            _screenTime.Start();
        }

        if (_settings.WeatherEnabled)
        {
            _weather.Start();
        }
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;

        _statsTimer.Interval = StatsInterval(settings);
        _focus.SetDuration(settings.FocusMinutes);

        ApplyModuleSwitches();

        RefreshAgenda();
        RefreshNotes();
        RaiseSystemProperties();
        Raise(nameof(Greeting));
        Raise(nameof(DisplayName));
        Raise(nameof(ToggleHotkeyHint));
        Raise(nameof(SettingsHotkeyHint));
        RaiseWeather();
    }

    /// <summary>Starts or stops the opt-in collectors to match the current settings.</summary>
    public void ApplyModuleSwitches()
    {
        if (_settings.ClipboardHistoryEnabled)
        {
            _clipboard.Start();
        }
        else
        {
            _clipboard.Stop();
            _clipboard.Clear();
        }

        if (_settings.ScreenTimeEnabled)
        {
            _screenTime.Start();
        }
        else
        {
            _screenTime.Stop();
        }

        if (_settings.WeatherEnabled)
        {
            _weather.Start();
        }
        else
        {
            _weather.Stop();
            _weatherSnapshot = WeatherSnapshot.Empty;
            _weatherError = null;
            RaiseWeather();
        }

        RebuildModules();
    }

    public void RefreshWeatherNow() => _ = _weather.RefreshAsync();

    private static TimeSpan StatsInterval(AppSettings settings) =>
        TimeSpan.FromMilliseconds(Math.Clamp(settings.StatsIntervalMs, 1000, 30000));

    /* ------------------------------------------------------------------ */
    /*  Identity / clock                                                   */
    /* ------------------------------------------------------------------ */

    public string DisplayName =>
        string.IsNullOrWhiteSpace(_settings.DisplayName)
            ? Environment.UserName
            : _settings.DisplayName.Trim();

    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                < 5 => "A quiet start.",
                < 12 => "Good morning",
                < 17 => "Good afternoon",
                < 21 => "Good evening",
                _ => "Winding down"
            };
        }
    }

    public string TimeText => DateTime.Now.ToString("HH:mm", Culture);

    public string DateText => DateTime.Now.ToString("dddd, d MMMM", Culture);

    public string ShortDateText => DateTime.Now.ToString("ddd, d MMM", Culture);

    public string ToggleHotkeyHint => _settings.ToggleHotkey;

    public string SettingsHotkeyHint => _settings.SettingsHotkey;

    private void RaiseClock()
    {
        Raise(nameof(TimeText));
        Raise(nameof(DateText));
        Raise(nameof(ShortDateText));
        Raise(nameof(Greeting));
    }

    /* ------------------------------------------------------------------ */
    /*  Modules                                                            */
    /* ------------------------------------------------------------------ */

    private readonly HashSet<DashboardModule> _enabled = new();

    public bool IsEnabled(DashboardModule module) => _enabled.Contains(module);

    public bool SystemVisible => IsEnabled(DashboardModule.System);

    public bool ClockVisible => IsEnabled(DashboardModule.Clock);

    public bool TimersVisible => IsEnabled(DashboardModule.Timers);

    public bool CalendarVisible => IsEnabled(DashboardModule.Calendar);

    public bool WeatherVisible => IsEnabled(DashboardModule.Weather);

    public bool ClipboardVisible => IsEnabled(DashboardModule.Clipboard);

    public bool NotesVisible => IsEnabled(DashboardModule.Notes);

    public bool SoundtrackVisible => IsEnabled(DashboardModule.Soundtrack);

    public string EnabledModuleCount => $"{_enabled.Count} little helpers, enabled";

    private void RebuildModules()
    {
        var wanted = _settings.EnabledModules is { Count: > 0 }
            ? _settings.EnabledModules
                .Select(key => Enum.TryParse<DashboardModule>(key, ignoreCase: true, out var module) ? module : (DashboardModule?)null)
                .Where(module => module is not null)
                .Select(module => module!.Value)
                .ToHashSet()
            : ModuleDefaults();

        _enabled.Clear();
        foreach (var module in wanted)
        {
            _enabled.Add(module);
        }

        Modules.Clear();
        foreach (var info in ModuleInfo.All)
        {
            Modules.Add(info);
        }

        Raise(nameof(SystemVisible));
        Raise(nameof(ClockVisible));
        Raise(nameof(TimersVisible));
        Raise(nameof(CalendarVisible));
        Raise(nameof(WeatherVisible));
        Raise(nameof(ClipboardVisible));
        Raise(nameof(NotesVisible));
        Raise(nameof(SoundtrackVisible));
        Raise(nameof(EnabledModuleCount));
    }

    private HashSet<DashboardModule> ModuleDefaults() =>
        ModuleInfo.Defaults.Concat(
                _settings.ClipboardHistoryEnabled ? new[] { DashboardModule.Clipboard } : Array.Empty<DashboardModule>())
            .ToHashSet();

    public void SetModuleEnabled(DashboardModule module, bool enabled)
    {
        if (enabled)
        {
            _enabled.Add(module);
        }
        else
        {
            _enabled.Remove(module);
        }

        _settings.EnabledModules = _enabled.Select(value => value.ToString()).ToList();

        // Clipboard visibility and clipboard consent are the same switch: turning the module
        // on is the consent, turning it off also clears what was captured.
        if (module == DashboardModule.Clipboard)
        {
            _settings.ClipboardHistoryEnabled = enabled;
        }

        ApplyModuleSwitches();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when a module toggle should be persisted.</summary>
    public event EventHandler? SettingsChanged;

    /* ------------------------------------------------------------------ */
    /*  System                                                             */
    /* ------------------------------------------------------------------ */

    public string CpuText => _snapshot.CpuText;

    public double CpuPercent => _snapshot.CpuPercent;

    public string MemoryText => $"{_snapshot.MemoryPercent:0}%";

    public double MemoryPercent => _snapshot.MemoryPercent;

    public string MemoryDetail =>
        _snapshot.TotalMemoryGb <= 0
            ? "—"
            : $"{_snapshot.UsedMemoryGb:0.0} / {_snapshot.TotalMemoryGb:0}G";

    public string BatteryText => _power.Display;

    public string BatteryState => _power.StateLabel;

    public bool BatteryLow => _power.IsLow;

    public int BatteryPercent => Math.Clamp(_power.Percent, 0, 100);

    public bool HasBattery => _power.HasBattery;

    private void RefreshSystem()
    {
        _snapshot = _stats.Sample();
        _power = _battery.Read();
        RaiseSystemProperties();
    }

    private void RaiseSystemProperties()
    {
        Raise(nameof(CpuText));
        Raise(nameof(CpuPercent));
        Raise(nameof(MemoryText));
        Raise(nameof(MemoryPercent));
        Raise(nameof(MemoryDetail));
        Raise(nameof(BatteryText));
        Raise(nameof(BatteryState));
        Raise(nameof(BatteryLow));
        Raise(nameof(BatteryPercent));
        Raise(nameof(HasBattery));
    }

    /* ------------------------------------------------------------------ */
    /*  Focus timer                                                        */
    /* ------------------------------------------------------------------ */

    public string FocusText => _focusState.Display;

    public string FocusActionLabel => _focusState.PrimaryActionLabel;

    public double FocusPercent => _focusState.RemainingPercent;

    public int FocusMinutes => (int)_focusState.Total.TotalMinutes;

    public void ToggleFocus() => _focus.Toggle();

    public void ResetFocus() => _focus.Reset();

    public void StartFocus()
    {
        if (!_focus.IsRunning)
        {
            _focus.Start();
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Weather                                                            */
    /* ------------------------------------------------------------------ */

    public bool WeatherHasData => _weatherSnapshot.HasData;

    public string WeatherLocation => _settings.WeatherLocation;

    public string WeatherTemperature =>
        _weatherSnapshot.HasData
            ? $"{Math.Round(_weatherSnapshot.ToDisplay(_weatherSnapshot.TemperatureC, _settings.WeatherUseFahrenheit)):0}°"
            : "—";

    public string WeatherDescription =>
        _weatherSnapshot.HasData ? WeatherCodes.Describe(_weatherSnapshot.Code) : "Weather unavailable";

    public string WeatherAside =>
        _weatherSnapshot.HasData ? WeatherCodes.Aside(_weatherSnapshot.Code) : (_weatherError ?? "Enable weather in Settings.");

    public string WeatherGlyph => WeatherCodes.Glyph(_weatherSnapshot.HasData ? _weatherSnapshot.Code : 3);

    public string WeatherHigh =>
        _weatherSnapshot.HasData
            ? $"H: {Math.Round(_weatherSnapshot.ToDisplay(_weatherSnapshot.HighC, _settings.WeatherUseFahrenheit)):0}°"
            : "H: —";

    public string WeatherLow =>
        _weatherSnapshot.HasData
            ? $"L: {Math.Round(_weatherSnapshot.ToDisplay(_weatherSnapshot.LowC, _settings.WeatherUseFahrenheit)):0}°"
            : "L: —";

    public string WeatherHumidity =>
        _weatherSnapshot.HasData ? $"{_weatherSnapshot.HumidityPercent}%" : "—";

    public string WeatherWind =>
        _weatherSnapshot.HasData
            ? $"{_weatherSnapshot.WindKph:0} km/h {WeatherCodes.CompassPoint(_weatherSnapshot.WindDirectionDegrees)}"
            : "—";

    public string WeatherUpdated =>
        _weatherSnapshot.HasData
            ? $"Updated {_weatherSnapshot.UpdatedAt:HH:mm}"
            : string.Empty;

    private void RaiseWeather()
    {
        Raise(nameof(WeatherHasData));
        Raise(nameof(WeatherLocation));
        Raise(nameof(WeatherTemperature));
        Raise(nameof(WeatherDescription));
        Raise(nameof(WeatherAside));
        Raise(nameof(WeatherGlyph));
        Raise(nameof(WeatherHigh));
        Raise(nameof(WeatherLow));
        Raise(nameof(WeatherHumidity));
        Raise(nameof(WeatherWind));
        Raise(nameof(WeatherUpdated));
    }

    /* ------------------------------------------------------------------ */
    /*  Clipboard                                                          */
    /* ------------------------------------------------------------------ */

    public bool ClipboardHasItems => _clipboard.Count > 0;

    public string ClipboardPreview =>
        _clipboard.Count == 0
            ? "Nothing copied yet. Copy something and it appears here."
            : _clipboard.Entries[0].IsSensitive
                ? ClipboardText.MaskPreview(_clipboard.Entries[0].Text)
                : ClipboardText.Preview(_clipboard.Entries[0].Text);

    public string ClipboardAge =>
        _clipboard.Count == 0
            ? "Off until you enable it"
            : ClipboardText.DescribeAge(DateTimeOffset.Now - _clipboard.Entries[0].CapturedAt);

    public string ClipboardCount => ClipboardText.CountLabel(_clipboard.Count);

    public IReadOnlyList<ClipboardEntry> ClipboardEntries => _clipboard.Entries;

    private void RaiseClipboard()
    {
        Raise(nameof(ClipboardHasItems));
        Raise(nameof(ClipboardPreview));
        Raise(nameof(ClipboardAge));
        Raise(nameof(ClipboardCount));
        Raise(nameof(ClipboardEntries));
    }

    public void ClearClipboard() => _clipboard.Clear();

    /* ------------------------------------------------------------------ */
    /*  Screen time                                                        */
    /* ------------------------------------------------------------------ */

    public string ScreenTimeTotal => ScreenTimeAggregator.FormatDuration(_screenTime.Today);

    public string ScreenTimeTrend
    {
        get
        {
            var trend = ScreenTimeAggregator.TrendPercent(_screenTime.Today, _screenTime.Yesterday);
            return trend is null ? "—" : $"{trend:+0;-0;0}%";
        }
    }

    public string ScreenTimeNote =>
        ScreenTimeAggregator.Encouragement(_screenTime.Today, _screenTime.Yesterday);

    public IReadOnlyList<double> ScreenTimeWeek => _screenTime.WeekBars;

    public string ScreenTimeFooter =>
        _settings.ScreenTimeEnabled
            ? "Stays on your device · totals only"
            : "Off — enable in Settings to see totals";

    private void RaiseScreenTime()
    {
        TopApps.Clear();
        foreach (var app in _screenTime.TodayApps.Take(4))
        {
            TopApps.Add(app);
        }

        Raise(nameof(ScreenTimeTotal));
        Raise(nameof(ScreenTimeTrend));
        Raise(nameof(ScreenTimeNote));
        Raise(nameof(ScreenTimeWeek));
        Raise(nameof(ScreenTimeFooter));
    }

    /* ------------------------------------------------------------------ */
    /*  Notes                                                              */
    /* ------------------------------------------------------------------ */

    public string LatestNoteTitle => _notes.Latest?.Title ?? "No notes yet";

    public string LatestNoteBody =>
        _notes.Latest is null
            ? "Jot something down — it stays on this PC."
            : ClipboardText.Preview(_notes.Latest.Body, 120);

    public string NotesCount => _notes.Notes.Count == 1 ? "1 note" : $"{_notes.Notes.Count} notes";

    public IReadOnlyList<NoteEntry> AllNotes => _notes.Notes;

    public NoteEntry AddNote(string title, string body) => _notes.Add(title, body);

    public void DeleteNote(Guid id) => _notes.Delete(id);

    private void RefreshNotes()
    {
        RecentNotes.Clear();
        foreach (var note in _notes.Notes.Take(3))
        {
            RecentNotes.Add(note);
        }

        RaiseNotes();
    }

    private void RaiseNotes()
    {
        RecentNotes.Clear();
        foreach (var note in _notes.Notes.Take(3))
        {
            RecentNotes.Add(note);
        }

        Raise(nameof(LatestNoteTitle));
        Raise(nameof(LatestNoteBody));
        Raise(nameof(NotesCount));
        Raise(nameof(AllNotes));
    }

    /* ------------------------------------------------------------------ */
    /*  Agenda                                                             */
    /* ------------------------------------------------------------------ */

    public string AgendaCountLabel
    {
        get
        {
            var remaining = DayPlan.RemainingToday(CurrentPlan(), TimeOnly.FromDateTime(DateTime.Now));
            return remaining switch
            {
                0 => "Nothing left today",
                1 => "1 ahead",
                _ => $"{remaining} ahead"
            };
        }
    }

    private IReadOnlyList<PlanEntry> CurrentPlan()
    {
        var fromFile = _agenda.GetPlan();
        return fromFile.Count > 0 ? fromFile : DayPlan.Sample;
    }

    public void RefreshAgenda()
    {
        var (upcoming, _) = DayPlan.Split(CurrentPlan(), TimeOnly.FromDateTime(DateTime.Now));

        UpNext.Clear();
        foreach (var entry in upcoming.Take(4))
        {
            UpNext.Add(entry with { Time = DayPlan.FormatClock(entry.Time) });
        }

        Raise(nameof(AgendaCountLabel));
        Raise(nameof(UpNext));
    }

    /* ------------------------------------------------------------------ */
    /*  Quotes                                                             */
    /* ------------------------------------------------------------------ */

    public string QuoteText => _quote;

    public string QuoteSource => _quoteSource;

    public void NextQuote()
    {
        var index = Quotes.ToList().FindIndex(pair => pair.Text == _quote);
        var next = Quotes[(index + 1 + Quotes.Count) % Quotes.Count];
        _quote = next.Text;
        _quoteSource = next.Source;
        Raise(nameof(QuoteText));
        Raise(nameof(QuoteSource));
    }

    /* ------------------------------------------------------------------ */

    private void Raise([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _clockTimer.Stop();
        _statsTimer.Stop();
        _focus.Dispose();
        _weather.Dispose();
        _clipboard.Dispose();
        _screenTime.Dispose();
        _stats.Dispose();
    }
}
