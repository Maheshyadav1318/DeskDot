using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskDot.Models;
using DeskDot.Services;
using DeskDot.ViewModels;
using DeskDot.Views;

namespace DeskDot;

/// <summary>
/// The DeskDot overlay: a borderless, per-monitor-DPI-aware, always-on-top window that
/// morphs between a collapsed pill and the full island panel.
/// </summary>
public partial class MainWindow : Window
{
    private const double CollapsedWidth = 404;
    private const double CollapsedHeight = 100;
    private const double ExpandedWidth = 800;
    private const double ExpandedHeight = 420;

    private static readonly Duration MorphDuration = new(TimeSpan.FromMilliseconds(420));
    private static readonly IEasingFunction MorphEase = new CubicEase { EasingMode = EasingMode.EaseOut };

    private readonly IslandViewModel _viewModel;
    private readonly AppSettings _settings;

    private HwndSource? _source;
    private HotkeyService? _hotkeys;
    private PowerNotificationService? _power;
    private TrayService? _tray;
    private SettingsWindow? _settingsWindow;
    private bool _isExpanded;
    private bool _isMorphing;
    private bool _allowClose;

    public MainWindow(AppSettings settings, AgendaService agenda)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(agenda);

        InitializeComponent();

        _viewModel = new IslandViewModel(settings, agenda);
        DataContext = _viewModel;
        _viewModel.FocusCompleted += OnFocusCompleted;

        Width = CollapsedWidth;
        Height = CollapsedHeight;
        Topmost = settings.AlwaysOnTop;

        Loaded += OnLoaded;
        Closing += OnClosing;
        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
        LocationChanged += (_, _) => PersistPosition();
    }

    /// <summary>Raised when the user asks to exit (tray menu) so the app can shut down.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Opened from other parts of the shell (tray menu / hotkey).</summary>
    public void ToggleIsland()
    {
        if (_isExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
    }

    public void ShowSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings.Clone());
        _settingsWindow.SettingsSaved += OnSettingsSaved;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /* ------------------------------------------------------------------ */
    /*  Lifecycle                                                          */
    /* ------------------------------------------------------------------ */

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _source = (HwndSource)PresentationSource.FromVisual(this)!;

        ApplyWindowChrome();
        RestorePosition();
        _viewModel.Start();

        _hotkeys = new HotkeyService(_source, ToggleIsland);
        _hotkeys.Register(_settings.ToggleHotkey);

        _power = new PowerNotificationService(_source, OnSystemResumed);

        _tray = new TrayService(ShowSettings, ToggleIsland, RequestExit);

        if (_settings.StartWithWindows != StartupService.IsEnabled())
        {
            StartupService.Apply(_settings.StartWithWindows);
        }
    }

    private void ApplyWindowChrome()
    {
        if (_source is null)
        {
            return;
        }

        var handle = _source.Handle;

        // No taskbar button, no Alt+Tab entry — DeskDot behaves like a shell overlay.
        var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        exStyle |= NativeMethods.WsExToolWindow;
        exStyle &= ~NativeMethods.WsExAppWindow;
        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, exStyle);

        // Rounded corners + dark backdrop on Windows 11; silently ignored on Windows 10.
        var corner = 2; // DWMWCP_ROUND
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var dark = 1;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmUseImmersiveDarkMode, ref dark, sizeof(int));

    }

    private void RestorePosition()
    {
        var workArea = SystemParameters.WorkArea;
        var left = _settings.IslandLeft ?? workArea.Left + (workArea.Width - Width) / 2;
        var top = _settings.IslandTop ?? workArea.Top + 12;

        left = Math.Clamp(left, workArea.Left - Width / 2, workArea.Right - Width / 2);
        top = Math.Clamp(top, workArea.Top - Height, workArea.Bottom - Height);

        Left = left;
        Top = top;
    }

    private void PersistPosition()
    {
        if (_isMorphing || !IsLoaded)
        {
            return;
        }

        _settings.IslandLeft = Left;
        _settings.IslandTop = Top;
    }

    private void OnSystemResumed()
    {
        // Sleep/resume drops hotkey registrations and stalls animations.
        _hotkeys?.ReRegister();
        _viewModel.Start();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isExpanded && _settings.CollapseOnFocusLoss)
        {
            Collapse();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isExpanded)
        {
            Collapse();
            e.Handled = true;
        }
    }

    /* ------------------------------------------------------------------ */
    /*  Morph between pill and panel                                       */
    /* ------------------------------------------------------------------ */

    public void Expand()
    {
        if (_isExpanded || _isMorphing)
        {
            return;
        }

        _isExpanded = true;
        _isMorphing = true;

        // Grow around the island's centre so it feels anchored to the notch area.
        var targetLeft = Left - (ExpandedWidth - CollapsedWidth) / 2;
        AnimateTo(ExpandedWidth, ExpandedHeight, targetLeft);

        CollapsedPanel.Visibility = Visibility.Collapsed;
        ExpandedPanel.Visibility = Visibility.Visible;
        AnimateOpacity(ExpandedPanel, 0, 1, TimeSpan.FromMilliseconds(260), TimeSpan.FromMilliseconds(120));

        Activate();
        Focus();
    }

    public void Collapse()
    {
        if (!_isExpanded || _isMorphing)
        {
            return;
        }

        _isExpanded = false;
        _isMorphing = true;

        var targetLeft = Left + (ExpandedWidth - CollapsedWidth) / 2;
        AnimateTo(CollapsedWidth, CollapsedHeight, targetLeft);
        AnimateOpacity(ExpandedPanel, 1, 0, TimeSpan.FromMilliseconds(120), TimeSpan.Zero);

        var restore = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
        restore.Tick += (_, _) =>
        {
            restore.Stop();
            if (!_isExpanded)
            {
                ExpandedPanel.Visibility = Visibility.Collapsed;
                CollapsedPanel.Visibility = Visibility.Visible;
            }
        };
        restore.Start();

        PersistPosition();
    }

    private void AnimateTo(double width, double height, double left)
    {
        var duration = MorphDuration;

        BeginAnimation(WidthProperty, new DoubleAnimation(width, duration) { EasingFunction = MorphEase });
        BeginAnimation(HeightProperty, new DoubleAnimation(height, duration) { EasingFunction = MorphEase });
        BeginAnimation(LeftProperty, new DoubleAnimation(left, duration) { EasingFunction = MorphEase });

        var done = new DispatcherTimer { Interval = duration.TimeSpan };
        done.Tick += (_, _) =>
        {
            done.Stop();
            BeginAnimation(WidthProperty, null);
            BeginAnimation(HeightProperty, null);
            BeginAnimation(LeftProperty, null);
            Width = width;
            Height = height;
            Left = left;
            _isMorphing = false;
            PersistPosition();
        };
        done.Start();
    }

    private static void AnimateOpacity(UIElement element, double from, double to, TimeSpan duration, TimeSpan delay)
    {
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(from, to, new Duration(duration))
        {
            BeginTime = delay,
            EasingFunction = MorphEase
        });
    }

    /* ------------------------------------------------------------------ */
    /*  Interaction                                                        */
    /* ------------------------------------------------------------------ */

    private void OnIslandMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isExpanded)
        {
            Expand();
            e.Handled = true;
            return;
        }

        // Dragging anywhere on the island chrome moves the overlay.
        if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) is not null)
        {
            return;
        }

        DragMove();
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T match)
            {
                return match;
            }

            node = node is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }

        return null;
    }

    private void OnMediaProgressClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.ActualWidth > 0)
        {
            var ratio = Math.Clamp(e.GetPosition(element).X / element.ActualWidth, 0, 1);
            _viewModel.SeekMedia(ratio * 100);
        }
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e) => _viewModel.TogglePlayback();

    private void OnNextClick(object sender, RoutedEventArgs e) => _viewModel.NextTrack();

    private void OnPreviousClick(object sender, RoutedEventArgs e) => _viewModel.PreviousTrack();

    private void OnFocusToggleClick(object sender, RoutedEventArgs e) => _viewModel.ToggleFocus();

    private void OnFocusResetClick(object sender, RoutedEventArgs e) => _viewModel.ResetFocus();

    private void OnFocusQuickClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartFocus();
        Collapse();
    }

    private void OnOpenSelfClick(object sender, RoutedEventArgs e) => OpenWithShell(AppContext.BaseDirectory);

    private void OnPowerClick(object sender, RoutedEventArgs e) =>
        OpenWithShell("ms-settings:batterysaver");

    private void OpenWithShell(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // Shell targets can be blocked by policy; ignore rather than crash.
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => ShowSettings();

    private void OnFocusCompleted(object? sender, EventArgs e)
    {
        _tray?.ShowBalloon("Focus session complete ✨", "Nice work — take a short break.");
    }

    /* ------------------------------------------------------------------ */
    /*  Settings + shutdown                                                */
    /* ------------------------------------------------------------------ */

    private void OnSettingsSaved(object? sender, AppSettings updated)
    {
        var previousHotkey = _settings.ToggleHotkey;
        _settings.ToggleHotkey = updated.ToggleHotkey;
        _settings.CollapseOnFocusLoss = updated.CollapseOnFocusLoss;
        _settings.AlwaysOnTop = updated.AlwaysOnTop;
        _settings.StartWithWindows = updated.StartWithWindows;
        _settings.StatsIntervalMs = updated.StatsIntervalMs;
        _settings.FocusMinutes = updated.FocusMinutes;
        _settings.DisplayName = updated.DisplayName;

        if (Topmost != _settings.AlwaysOnTop)
        {
            Topmost = _settings.AlwaysOnTop;
        }

        StartupService.Apply(_settings.StartWithWindows);
        if (!string.Equals(previousHotkey, _settings.ToggleHotkey, StringComparison.OrdinalIgnoreCase))
        {
            _hotkeys?.Register(_settings.ToggleHotkey);
        }

        _viewModel.ApplySettings(_settings);
    }

    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // Closing via Alt+F4 or the shell should only hide the island.
            e.Cancel = true;
            if (_isExpanded)
            {
                Collapse();
            }

            return;
        }

        _hotkeys?.Dispose();
        _power?.Dispose();
        _tray?.Dispose();
        _viewModel.Dispose();

        App.SaveSettings(_settings);
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
