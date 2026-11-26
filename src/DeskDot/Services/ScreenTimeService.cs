using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using DeskDot.Core;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Opt-in screen time: samples the foreground window every few seconds and keeps per-process
/// totals for today plus the last seven days. Only local, aggregate durations are stored
/// (<c>%APPDATA%\DeskDot\screentime.json</c>) — never window titles or content.
/// </summary>
public sealed class ScreenTimeService : IDisposable
{
    private readonly DispatcherTimer _sample;
    private readonly DispatcherTimer _save;
    private readonly Func<bool> _isEnabled;
    private readonly Dictionary<string, TimeSpan> _today = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DayTotal> _history = new();
    private readonly string _storePath;
    private string? _lastProcess;
    private DateTime _lastTick = DateTime.Now;
    private bool _dirty;
    private bool _disposed;

    public ScreenTimeService(Func<bool> isEnabled, string? storeDirectory = null)
    {
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));

        var directory = storeDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskDot");
        _storePath = Path.Combine(directory, "screentime.json");

        _sample = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(5) };
        _sample.Tick += (_, _) => Tick();

        _save = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(2) };
        _save.Tick += (_, _) => SaveIfDirty();
    }

    public event EventHandler? Changed;

    public IReadOnlyList<AppUsage> TodayApps { get; private set; } = Array.Empty<AppUsage>();

    public TimeSpan Today { get; private set; }

    public TimeSpan Yesterday { get; private set; }

    public IReadOnlyList<double> WeekBars { get; private set; } = Array.Empty<double>();

    public void Start()
    {
        Load();
        RollOverIfNeeded();
        Recalculate();
        _sample.Start();
        _save.Start();
    }

    public void Stop()
    {
        _sample.Stop();
        _save.Stop();
        SaveIfDirty();
    }

    private void Tick()
    {
        var now = DateTime.Now;
        var elapsed = now - _lastTick;
        _lastTick = now;

        // Ignore gaps that mean the machine was asleep or the app was suspended.
        if (elapsed > TimeSpan.FromMinutes(2) || elapsed <= TimeSpan.Zero)
        {
            return;
        }

        if (!_isEnabled())
        {
            return;
        }

        RollOverIfNeeded();

        var process = NativeMethods.GetForegroundProcessName();
        _lastProcess = process;

        if (!string.IsNullOrEmpty(process))
        {
            _today[process] = _today.TryGetValue(process, out var existing) ? existing + elapsed : elapsed;
            _dirty = true;
            Recalculate();
        }
    }

    private void RollOverIfNeeded()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var stored = _history.FirstOrDefault(day => day.Date == today);

        if (stored is null)
        {
            // Yesterday's totals move into history before we start a new day.
            if (_today.Count > 0)
            {
                _history.Add(new DayTotal(today.AddDays(-1), _today.ToDictionary(p => p.Key, p => p.Value)));
                _today.Clear();
            }

            _history.RemoveAll(day => day.Date < today.AddDays(-7));
        }
    }

    private void Recalculate()
    {
        var samples = _today.Select(pair => new ScreenTimeAggregator.Sample(pair.Key, pair.Value));
        var totals = ScreenTimeAggregator.Summarise(samples);

        TodayApps = totals
            .Select(total => new AppUsage(
                total.ProcessName,
                FriendlyName(total.ProcessName),
                total.Duration,
                total.Percent))
            .ToList();

        Today = _today.Values.Aggregate(TimeSpan.Zero, (sum, value) => sum + value);

        var yesterdayEntry = _history.FirstOrDefault(day => day.Date == DateOnly.FromDateTime(DateTime.Now).AddDays(-1));
        Yesterday = yesterdayEntry?.Apps
            .Where(pair => pair.Key != "deskdot")
            .Aggregate(TimeSpan.Zero, (sum, pair) => sum + pair.Value) ?? TimeSpan.Zero;

        var days = _history
            .OrderBy(day => day.Date)
            .Select(day => day.Apps.Aggregate(TimeSpan.Zero, (sum, pair) => sum + pair.Value))
            .ToList();

        days.Add(TimeSpan.Zero); // today is rendered from the live totals below
        days[^1] = Today;

        WeekBars = ScreenTimeAggregator.WeekBars(days);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Turns <c>code</c> into "VS Code" / "Chrome" style labels for the card.</summary>
    public static string FriendlyName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return "Unknown";
        }

        var name = processName.Trim();

        return name.ToLowerInvariant() switch
        {
            "code" => "VS Code",
            "chrome" or "google chrome" => "Chrome",
            "msedge" => "Edge",
            "firefox" => "Firefox",
            "explorer" => "File Explorer",
            "devenv" => "Visual Studio",
            "windowsterminal" or "wt" => "Terminal",
            "powershell" or "pwsh" => "PowerShell",
            "notepad" => "Notepad",
            "spotify" => "Spotify",
            "discord" => "Discord",
            "slack" => "Slack",
            "teams" or "ms-teams" => "Teams",
            "outlook" => "Outlook",
            "deskdot" => "DeskDot",
            _ => char.ToUpperInvariant(name[0]) + name[1..]
        };
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_storePath))
            {
                return;
            }

            var json = File.ReadAllText(_storePath);
            var stored = JsonSerializer.Deserialize<ScreenTimeStore>(json);
            if (stored is null)
            {
                return;
            }

            _history.Clear();
            _history.AddRange(stored.Days ?? new List<DayTotal>());

            var today = DateOnly.FromDateTime(DateTime.Now);
            var current = _history.FirstOrDefault(day => day.Date == today);
            if (current is not null)
            {
                foreach (var pair in current.Apps)
                {
                    _today[pair.Key] = pair.Value;
                }

                _history.Remove(current);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A damaged history file just starts a fresh week.
        }
    }

    private void SaveIfDirty()
    {
        if (!_dirty)
        {
            return;
        }

        Save();
    }

    public void Save()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var days = _history
                .Where(day => day.Date != today)
                .ToList();

            if (_today.Count > 0)
            {
                days.Add(new DayTotal(today, new Dictionary<string, TimeSpan>(_today)));
            }

            var store = new ScreenTimeStore { Days = days };
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
            var temp = _storePath + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _storePath, overwrite: true);
            _dirty = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // History is a nicety; failing to save must not surface to the user.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sample.Stop();
        _save.Stop();
        Save();
    }

    private sealed class ScreenTimeStore
    {
        public List<DayTotal>? Days { get; set; }
    }

    public sealed class DayTotal
    {
        public DateOnly Date { get; set; }

        public Dictionary<string, TimeSpan> Apps { get; set; } = new();

        public DayTotal()
        {
        }

        public DayTotal(DateOnly date, Dictionary<string, TimeSpan> apps)
        {
            Date = date;
            Apps = apps;
        }
    }
}
