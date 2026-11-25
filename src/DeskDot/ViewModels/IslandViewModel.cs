using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DeskDot.Models;
using DeskDot.Services;

namespace DeskDot.ViewModels;

/// <summary>Bindable state for the island. Owns the clock, stats, media and focus services.</summary>
public sealed class IslandViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;

    private readonly SystemStatsService _stats = new();
    private readonly MediaService _media = new();
    private readonly FocusTimerService _focus;
    private readonly AgendaService _agenda;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _statsTimer;
    private readonly DispatcherTimer _agendaTimer;

    private SystemSnapshot _snapshot = SystemSnapshot.Empty;
    private MediaState _mediaState = new(TrackInfo.Empty, 0, isPlaying: false);
    private FocusState _focusState;
    private AgendaItem _next = AgendaItem.None;
    private AppSettings _settings;
    private bool _disposed;

    public IslandViewModel(AppSettings settings, AgendaService agenda)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _agenda = agenda ?? throw new ArgumentNullException(nameof(agenda));

        _focus = new FocusTimerService(settings.FocusMinutes);
        _focusState = _focus.State;

        _media.StateChanged += (_, state) =>
        {
            _mediaState = state;
            OnPropertyChanged(nameof(TrackTitle));
            OnPropertyChanged(nameof(TrackArtist));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(PlayGlyph));
        };

        _focus.Changed += (_, state) =>
        {
            _focusState = state;
            OnPropertyChanged(nameof(FocusText));
            OnPropertyChanged(nameof(FocusActionLabel));
            OnPropertyChanged(nameof(FocusPercent));
        };

        _focus.Completed += (_, _) => FocusCompleted?.Invoke(this, EventArgs.Empty);

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(TimeText));
            OnPropertyChanged(nameof(DateText));
            OnPropertyChanged(nameof(Greeting));
        };

        _statsTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = StatsInterval(settings) };
        _statsTimer.Tick += (_, _) => RefreshStats();

        _agendaTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(1) };
        _agendaTimer.Tick += (_, _) => RefreshAgenda();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when a focus session finishes so the shell can notify the user.</summary>
    public event EventHandler? FocusCompleted;

    public void Start()
    {
        RefreshStats();
        RefreshAgenda();
        _media.Start();
        _clockTimer.Start();
        _statsTimer.Start();
        _agendaTimer.Start();

        OnPropertyChanged(nameof(TimeText));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(HotkeyHint));
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _statsTimer.Interval = StatsInterval(settings);
        _focus.SetDuration(settings.FocusMinutes);
        RefreshAgenda();
        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(HotkeyHint));
    }

    private static TimeSpan StatsInterval(AppSettings settings) =>
        TimeSpan.FromMilliseconds(Math.Clamp(settings.StatsIntervalMs, 1000, 30000));

    /* ---------------- Clock ---------------- */

    public string TimeText => DateTime.Now.ToString("HH:mm", Culture);

    public string DateText => DateTime.Now.ToString("ddd, MMM d", Culture);

    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var name = string.IsNullOrWhiteSpace(_settings.DisplayName) ? string.Empty : $", {_settings.DisplayName}";

            return hour switch
            {
                < 5 => $"Still up{name}? Take it easy. 🌙",
                < 12 => $"Good morning{name}! ✨",
                < 17 => $"Have a wonderful day{name}! ✨",
                < 21 => $"Good evening{name}! 🌇",
                _ => $"Winding down{name}. 🌙"
            };
        }
    }

    public string HotkeyHint => _settings.ToggleHotkey;

    /* ---------------- Agenda ---------------- */

    public string AgendaLabel => string.IsNullOrEmpty(_next.Time) ? "NEXT" : $"Next · {_next.Time}";

    public string AgendaTitle => _next.Title;

    /* ---------------- Media ---------------- */

    public string TrackTitle => _mediaState.Track.Title;

    public string TrackArtist => _mediaState.Track.Artist;

    public double ProgressPercent => _mediaState.PositionPercent;

    public string PlayGlyph => _mediaState.IsPlaying ? "\uE769" : "\uE768";

    public void TogglePlayback() => _media.TogglePlayback();

    public void NextTrack() => _media.Next();

    public void PreviousTrack() => _media.Previous();

    public void SeekMedia(double percent) => _media.Seek(percent);

    /* ---------------- Focus ---------------- */

    public string FocusText => _focusState.Display;

    public string FocusActionLabel => _focusState.PrimaryActionLabel;

    public double FocusPercent => _focusState.RemainingPercent;

    public void ToggleFocus() => _focus.Toggle();

    public void ResetFocus() => _focus.Reset();

    public void StartFocus()
    {
        if (!_focus.IsRunning)
        {
            _focus.Start();
        }
    }

    /* ---------------- System ---------------- */

    public string CpuText => _snapshot.CpuText;

    public double CpuPercent => _snapshot.CpuPercent;

    public string MemoryText => _snapshot.MemoryText;

    public string MemorySummary =>
        _snapshot.TotalMemoryGb <= 0 ? "—" : $"{_snapshot.UsedMemoryGb:0.0} / {_snapshot.TotalMemoryGb:0}G";

    public double MemoryPercent => _snapshot.MemoryPercent;

    private void RefreshStats()
    {
        _snapshot = _stats.Sample();
        OnPropertyChanged(nameof(CpuText));
        OnPropertyChanged(nameof(CpuPercent));
        OnPropertyChanged(nameof(MemoryText));
        OnPropertyChanged(nameof(MemorySummary));
        OnPropertyChanged(nameof(MemoryPercent));
    }

    private void RefreshAgenda()
    {
        _next = _agenda.GetNext(DateTime.Now);
        OnPropertyChanged(nameof(AgendaLabel));
        OnPropertyChanged(nameof(AgendaTitle));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
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
        _agendaTimer.Stop();
        _media.Dispose();
        _focus.Dispose();
        _stats.Dispose();
    }
}
