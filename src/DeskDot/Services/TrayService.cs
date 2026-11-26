using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

namespace DeskDot.Services;

/// <summary>Owns the notification-area icon and its context menu.</summary>
public sealed class TrayService : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private readonly bool _ownsIcon;
    private bool _disposed;

    public TrayService(Action openSettings, Action toggleIsland, Action exit)
    {
        (_icon, _ownsIcon) = LoadIcon();

        var menu = new WinForms.ContextMenuStrip
        {
            ShowImageMargin = false
        };
        menu.Items.Add(new WinForms.ToolStripMenuItem("Show / hide DeskDot", null, (_, _) => toggleIsland()));
        menu.Items.Add(new WinForms.ToolStripMenuItem("Settings…", null, (_, _) => openSettings()));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(new WinForms.ToolStripMenuItem("Exit DeskDot", null, (_, _) => exit()));

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = _icon,
            Text = "DeskDot — A little space. A better day.",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == WinForms.MouseButtons.Left)
            {
                toggleIsland();
            }
        };
        _notifyIcon.DoubleClick += (_, _) => openSettings();
    }

    /// <summary>Builds a tray icon from the embedded PNG; falls back to the generic app icon.</summary>
    private static (Icon Icon, bool Owns) LoadIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app-icon.png", UriKind.Absolute);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            using var png = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));
            encoder.Save(png);

            using var stream = new MemoryStream(png.ToArray());
            using var bitmap = new Bitmap(stream);

            var handle = bitmap.GetHicon();
            try
            {
                // Clone so the icon survives the GDI handle and the Bitmap being released.
                return ((Icon)Icon.FromHandle(handle).Clone(), true);
            }
            finally
            {
                NativeMethods.DestroyIcon(handle);
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or System.Runtime.InteropServices.ExternalException)
        {
            return (SystemIcons.Application, false);
        }
    }

    public void ShowBalloon(string title, string message)
    {
        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(4000);
        }
        catch (Exception)
        {
            // Balloon tips are decorative; never let them crash the overlay.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        if (_ownsIcon)
        {
            _icon.Dispose();
        }
    }
}
