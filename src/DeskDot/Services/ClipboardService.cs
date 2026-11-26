using System.Runtime.InteropServices;
using System.Windows;
using DeskDot.Core;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Opt-in clipboard history. Captures text only, keeps it in memory for the session (capped
/// at <see cref="ClipboardText.MaxEntries"/>) and never writes it to disk. Masking of
/// secret-looking entries is decided here so the UI cannot forget.
/// </summary>
public sealed class ClipboardService : IDisposable
{
    private readonly List<ClipboardEntry> _entries = new();
    private readonly Func<bool> _isEnabled;
    private readonly Func<bool> _maskSensitive;
    private readonly DispatcherTimer _poll;
    private string? _lastSeen;
    private bool _disposed;

    public ClipboardService(Func<bool> isEnabled, Func<bool> maskSensitive)
    {
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        _maskSensitive = maskSensitive ?? throw new ArgumentNullException(nameof(maskSensitive));

        // WPF has no clipboard-changed event, so poll; 1.2 s is cheap and unobtrusive.
        _poll = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(1200) };
        _poll.Tick += (_, _) => Poll();
    }

    public event EventHandler? Changed;

    public IReadOnlyList<ClipboardEntry> Entries => _entries;

    public int Count => _entries.Count;

    public void Start() => _poll.Start();

    public void Stop() => _poll.Stop();

    /// <summary>Clears history immediately (used when the user turns the module off).</summary>
    public void Clear()
    {
        _entries.Clear();
        _lastSeen = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Poll()
    {
        if (!_isEnabled())
        {
            return;
        }

        string? text;
        try
        {
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                return;
            }

            text = Clipboard.GetText(TextDataFormat.UnicodeText);
        }
        catch (ExternalException)
        {
            // Another process owns the clipboard; try again on the next tick.
            return;
        }
        catch (Exception)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, _lastSeen, StringComparison.Ordinal))
        {
            return;
        }

        _lastSeen = text;

        var stored = text.Length > ClipboardText.MaxStoredLength
            ? text[..ClipboardText.MaxStoredLength]
            : text;

        var sensitive = ClipboardText.LooksSensitive(stored);
        _entries.Insert(0, new ClipboardEntry(stored, DateTimeOffset.Now, sensitive && _maskSensitive()));

        while (_entries.Count > ClipboardText.MaxEntries)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _poll.Stop();
    }
}
