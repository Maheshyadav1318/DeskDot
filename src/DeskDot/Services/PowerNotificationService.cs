using System.Windows.Interop;

namespace DeskDot.Services;

/// <summary>
/// Listens for display / power / session-change events so the island can drop its
/// animations while the machine sleeps and re-acquire its hotkey after resume.
/// </summary>
public sealed class PowerNotificationService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Action _onResume;
    private bool _disposed;

    public PowerNotificationService(HwndSource source, Action onResume)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _onResume = onResume ?? throw new ArgumentNullException(nameof(onResume));

        if (!NativeMethods.WtsRegisterSessionNotification(_source.Handle, NativeMethods.NotifyForThisSession))
        {
            // Non-fatal: power notifications simply stay unavailable.
        }

        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeMethods.WmPowerBroadcast:
                var evt = wParam.ToInt32();
                if (evt is NativeMethods.PbtApmResumeAutomatic or NativeMethods.PbtApmResumeSuspend)
                {
                    _onResume();
                    handled = true;
                }
                else if (evt is NativeMethods.PbtApmSuspend)
                {
                    handled = true;
                }

                break;

            case NativeMethods.WmWtsSessionChange:
                var sessionEvt = wParam.ToInt32();
                if (sessionEvt is NativeMethods.WtsSessionUnlock or NativeMethods.WtsSessionLogon or NativeMethods.WtsConsoleConnect)
                {
                    _onResume();
                    handled = true;
                }

                break;
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
        _source.RemoveHook(WndProc);
        NativeMethods.WtsUnregisterSessionNotification(_source.Handle);
    }
}
