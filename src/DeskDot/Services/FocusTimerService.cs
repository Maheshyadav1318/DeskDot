using DeskDot.Core;

namespace DeskDot.Services;

/// <summary>Pomodoro-style focus countdown driving the island's focus card.</summary>
public sealed class FocusTimerService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _remaining;
    private bool _disposed;

    public FocusTimerService(int minutes)
    {
        Total = FocusClock.ClampDuration(minutes);
        _remaining = Total;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
    }

    public event EventHandler<FocusState>? Changed;

    /// <summary>Raised once when the countdown reaches zero.</summary>
    public event EventHandler? Completed;

    public TimeSpan Total { get; private set; }

    public bool IsRunning => _timer.IsEnabled;

    public TimeSpan Remaining => _remaining;

    public FocusState State => new(_remaining, Total, IsRunning);

    public void SetDuration(int minutes)
    {
        Total = FocusClock.ClampDuration(minutes);
        Reset();
    }

    public void Start()
    {
        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = Total;
        }

        _timer.Start();
        Publish();
    }

    public void Pause()
    {
        _timer.Stop();
        Publish();
    }

    public void Toggle()
    {
        if (IsRunning)
        {
            Pause();
        }
        else
        {
            Start();
        }
    }

    public void Reset()
    {
        _timer.Stop();
        _remaining = Total;
        Publish();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _remaining -= TimeSpan.FromSeconds(1);
        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            _timer.Stop();
            Publish();
            Completed?.Invoke(this, EventArgs.Empty);
            return;
        }

        Publish();
    }

    private void Publish() => Changed?.Invoke(this, State);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}

/// <summary>Snapshot of the focus countdown.</summary>
public sealed record FocusState(TimeSpan Remaining, TimeSpan Total, bool IsRunning)
{
    public double RemainingPercent => FocusClock.RemainingPercent(Remaining, Total);

    public string Display => FocusClock.Format(Remaining);

    public string PrimaryActionLabel => IsRunning ? "Pause" : "Start";
}
