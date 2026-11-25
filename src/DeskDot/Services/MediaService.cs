using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Now-playing source for the island.
///
/// v1.0 ships a bundled demo playlist so the overlay always has something to show and so
/// the UI can be exercised without any media player running. The public surface
/// (TogglePlayback / Next / Previous / Seek + <see cref="StateChanged"/>) is the seam where
/// Windows System Media Transport Controls (GSMTC) support will plug in for v1.1 — see
/// docs/ROADMAP.md.
/// </summary>
public sealed class MediaService : IDisposable
{
    private static readonly TrackInfo[] DemoTracks =
    {
        new("Amber Stillness", "Ólafur & Kiasmos", "Focus"),
        new("Quiet Horizons", "Nils Frahm"),
        new("Night Circuit", "Bonobo"),
        new("Soft Focus", "Tycho"),
        new("Afterglow", "Lusine")
    };

    private readonly DispatcherTimer _timer;
    private int _index;
    private double _position = 32;
    private bool _isPlaying = true;
    private bool _disposed;

    public MediaService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Advance();
    }

    /// <summary>Raised roughly once a second while playback advances.</summary>
    public event EventHandler<MediaState>? StateChanged;

    public MediaState Current { get; private set; } =
        new(DemoTracks[0], 32, isPlaying: true);

    public void Start()
    {
        Publish();
        _timer.Start();
    }

    public void TogglePlayback()
    {
        _isPlaying = !_isPlaying;
        Publish();
    }

    public void Next()
    {
        _index = (_index + 1) % DemoTracks.Length;
        _position = 0;
        _isPlaying = true;
        Publish();
    }

    public void Previous()
    {
        _index = (_index - 1 + DemoTracks.Length) % DemoTracks.Length;
        _position = 0;
        _isPlaying = true;
        Publish();
    }

    public void Seek(double percent)
    {
        _position = Math.Clamp(percent, 0, 100);
        Publish();
    }

    public void Refresh() => Publish();

    private void Advance()
    {
        if (!_isPlaying)
        {
            return;
        }

        _position += 0.35;
        if (_position >= 100)
        {
            _position = 0;
            _index = (_index + 1) % DemoTracks.Length;
        }

        Publish();
    }

    private void Publish()
    {
        Current = new MediaState(DemoTracks[_index], _position, _isPlaying);
        StateChanged?.Invoke(this, Current);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
    }
}

/// <summary>Immutable view of the current now-playing state.</summary>
public sealed class MediaState
{
    public MediaState(TrackInfo track, double positionPercent, bool isPlaying)
    {
        Track = track;
        PositionPercent = Math.Clamp(positionPercent, 0, 100);
        IsPlaying = isPlaying;
    }

    public TrackInfo Track { get; }

    public double PositionPercent { get; }

    public bool IsPlaying { get; }
}
