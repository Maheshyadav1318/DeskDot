using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace DeskDot.Services;

/// <summary>
/// Registers a system-wide hotkey on the island window and raises <see cref="Pressed"/>.
/// Re-registration is supported so settings changes (and sleep/resume) can rebind safely.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0xD0D0;

    private readonly HwndSource _source;
    private readonly Action _onPressed;
    private uint _modifiers;
    private uint _virtualKey;
    private string _gesture = "Alt+Space";
    private bool _registered;
    private bool _disposed;

    public HotkeyService(HwndSource source, Action onPressed)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        _source.AddHook(WndProc);
    }

    /// <summary>Parses "Alt+Space"-style text and registers it. Falls back to Alt+Space.</summary>
    public bool Register(string gesture)
    {
        Unregister();
        _gesture = string.IsNullOrWhiteSpace(gesture) ? "Alt+Space" : gesture;

        if (!TryParse(gesture, out _modifiers, out _virtualKey))
        {
            if (!TryParse("Alt+Space", out _modifiers, out _virtualKey))
            {
                return false;
            }
        }

        _registered = NativeMethods.RegisterHotKey(
            _source.Handle,
            HotkeyId,
            _modifiers | NativeMethods.ModNoRepeat,
            _virtualKey);

        if (!_registered &&
            TryParse("Alt+Space", out var fallbackMods, out var fallbackKey) &&
            (fallbackMods != _modifiers || fallbackKey != _virtualKey))
        {
            // Another app owns the requested gesture — retry the documented default.
            _modifiers = fallbackMods;
            _virtualKey = fallbackKey;
            _registered = NativeMethods.RegisterHotKey(
                _source.Handle,
                HotkeyId,
                _modifiers | NativeMethods.ModNoRepeat,
                _virtualKey);
        }

        if (_registered)
        {
            // Store the canonical spelling so resume re-registration asks for the same keys.
            _gesture = Describe();
        }

        return _registered;
    }

    /// <summary>Called after power resume / session unlock to re-acquire the gesture.</summary>
    public void ReRegister() => Register(_gesture);

    public string Describe()
    {
        var parts = new List<string>(4);
        if ((_modifiers & NativeMethods.ModControl) != 0) parts.Add("Ctrl");
        if ((_modifiers & NativeMethods.ModAlt) != 0) parts.Add("Alt");
        if ((_modifiers & NativeMethods.ModShift) != 0) parts.Add("Shift");
        if ((_modifiers & NativeMethods.ModWin) != 0) parts.Add("Win");
        parts.Add(KeyInterop.KeyFromVirtualKey((int)_virtualKey).ToString());
        return string.Join("+", parts);
    }

    public bool IsRegistered => _registered;

    private void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _onPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    internal static bool TryParse(string? gesture, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(gesture))
        {
            return false;
        }

        var tokens = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        foreach (var token in tokens)
        {
            switch (token.ToLowerInvariant())
            {
                case "alt":
                    modifiers |= NativeMethods.ModAlt;
                    break;
                case "ctrl":
                case "control":
                    modifiers |= NativeMethods.ModControl;
                    break;
                case "shift":
                    modifiers |= NativeMethods.ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= NativeMethods.ModWin;
                    break;
                default:
                    if (!Enum.TryParse<Key>(token, ignoreCase: true, out var key) ||
                        key == Key.None ||
                        key == Key.LeftAlt || key == Key.RightAlt ||
                        key == Key.LeftCtrl || key == Key.RightCtrl ||
                        key == Key.LeftShift || key == Key.RightShift)
                    {
                        return false;
                    }

                    virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
                    break;
            }
        }

        return modifiers != 0 && virtualKey != 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Unregister();
        _source.RemoveHook(WndProc);
    }
}
