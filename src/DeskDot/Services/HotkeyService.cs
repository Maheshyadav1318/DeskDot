using System.Windows.Interop;
using DeskDot.Core;

namespace DeskDot.Services;

/// <summary>
/// Registers a system-wide hotkey on the island window and raises <see cref="Pressed"/>.
/// String parsing/formatting is delegated to <see cref="HotkeyGesture"/>; this class only
/// touches Win32, so it can re-register after sleep/resume or a settings change.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0xD0D0;

    private readonly HwndSource _source;
    private readonly Action _onPressed;
    private uint _modifiers;
    private uint _virtualKey;
    private string _gesture = HotkeyGesture.Default;
    private bool _registered;
    private bool _disposed;

    public HotkeyService(HwndSource source, Action onPressed)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _onPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        _source.AddHook(WndProc);
    }

    /// <summary>
    /// Parses and registers <paramref name="gesture"/>, falling back to Alt+Space when the
    /// text is invalid or another application already owns the combination.
    /// </summary>
    public bool Register(string gesture)
    {
        Unregister();

        if (!HotkeyGesture.TryParse(gesture, out _modifiers, out _virtualKey) &&
            !HotkeyGesture.TryParse(HotkeyGesture.Default, out _modifiers, out _virtualKey))
        {
            return false;
        }

        _registered = TryRegister();

        if (!_registered && HotkeyGesture.TryParse(HotkeyGesture.Default, out var mods, out var key) &&
            (mods != _modifiers || key != _virtualKey))
        {
            _modifiers = mods;
            _virtualKey = key;
            _registered = TryRegister();
        }

        if (_registered)
        {
            // Cache the canonical spelling so resume asks for exactly the same keys.
            _gesture = HotkeyGesture.Format(_modifiers, _virtualKey);
        }

        return _registered;
    }

    /// <summary>Re-acquires the gesture after power resume or session unlock.</summary>
    public void ReRegister() => Register(_gesture);

    public bool IsRegistered => _registered;

    /// <summary>Canonical text for the currently registered gesture.</summary>
    public string Describe() => HotkeyGesture.Format(_modifiers, _virtualKey);

    private bool TryRegister() => NativeMethods.RegisterHotKey(
        _source.Handle,
        HotkeyId,
        _modifiers | NativeMethods.ModNoRepeat,
        _virtualKey);

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
