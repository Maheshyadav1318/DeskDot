using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using DeskDot.Models;
using DeskDot.Services;
using DeskDot.ViewModels;
using DeskDot.Views;

namespace DeskDot;

/// <summary>
/// The DeskDot dashboard window: a light, calm page of cards with a tray presence, a global
/// show/hide hotkey, remembered geometry and sleep/resume resilience.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly AppSettings _settings;

    private HwndSource? _source;
    private HotkeyService? _toggleHotkey;
    private HotkeyService? _settingsHotkey;
    private PowerNotificationService? _power;
    private TrayService? _tray;
    private SettingsWindow? _settingsWindow;
    private bool _allowClose;

    public MainWindow(AppSettings settings, AgendaService agenda, NotesService notes)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        InitializeComponent();

        _viewModel = new DashboardViewModel(settings, agenda, notes);
        DataContext = _viewModel;
        _viewModel.FocusCompleted += OnFocusCompleted;
        _viewModel.SettingsChanged += (_, _) => App.SaveSettings(_settings);

        Topmost = settings.AlwaysOnTop;

        Loaded += OnLoaded;
        Closing += OnClosing;
        StateChanged += (_, _) => PersistGeometry();
    }

    /// <summary>Raised when the app should shut down (tray menu).</summary>
    public event EventHandler? ExitRequested;

    private void OnLoaded(object sender, EventArgs e)
    {
        _source = (HwndSource)PresentationSource.FromVisual(this)!;

        ApplyWindowChrome();
        RestoreGeometry();
        _viewModel.Start();

        _toggleHotkey = new HotkeyService(_source, ToggleWindow);
        _toggleHotkey.Register(_settings.ToggleHotkey);

        _settingsHotkey = new HotkeyService(_source, ShowSettings, id: 0xD0D1);
        _settingsHotkey.Register(_settings.SettingsHotkey);

        _power = new PowerNotificationService(_source, OnSystemResumed);
        _tray = new TrayService(ShowSettings, ShowWindow, RequestExit);

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

        // Rounded corners on Windows 11; silently ignored on Windows 10.
        var handle = _source.Handle;
        var corner = 2;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref corner, sizeof(int));

        var light = 0;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmUseImmersiveDarkMode, ref light, sizeof(int));
    }

    /* ------------------------------------------------------------------ */
    /*  Geometry                                                           */
    /* ------------------------------------------------------------------ */

    private void RestoreGeometry()
    {
        var work = SystemParameters.WorkArea;

        if (_settings.WindowWidth is { } width && _settings.WindowHeight is { } height)
        {
            Width = Math.Clamp(width, MinWidth, SystemParameters.VirtualScreenWidth);
            Height = Math.Clamp(height, MinHeight, SystemParameters.VirtualScreenHeight);
        }
        else
        {
            Width = Math.Min(1160, work.Width - 80);
            Height = Math.Min(880, work.Height - 80);
        }

        if (_settings.WindowLeft is { } left && _settings.WindowTop is { } top)
        {
            // Only restore a position that is still on a visible monitor.
            var stillVisible = left + 80 < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
                               && left > SystemParameters.VirtualScreenLeft - Width + 80
                               && top + 40 < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight
                               && top > SystemParameters.VirtualScreenTop - Height + 40;

            if (stillVisible)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top = top;
            }
        }

        if (_settings.WindowMaximised)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void PersistGeometry()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (WindowState == WindowState.Normal)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }

        _settings.WindowMaximised = WindowState == WindowState.Maximized;
    }

    /* ------------------------------------------------------------------ */
    /*  Show / hide / hotkeys                                              */
    /* ------------------------------------------------------------------ */

    public void ToggleWindow()
    {
        if (IsVisible && WindowState != WindowState.Minimized)
        {
            HideToTray();
        }
        else
        {
            ShowWindow();
        }
    }

    public void ShowWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }

    private void HideToTray()
    {
        PersistGeometry();
        Hide();
    }

    private void OnSystemResumed()
    {
        // Sleep/resume and unlock drop hotkey registrations.
        _toggleHotkey?.ReRegister();
        _settingsHotkey?.ReRegister();
    }

    /* ------------------------------------------------------------------ */
    /*  Card actions                                                       */
    /* ------------------------------------------------------------------ */

    private void OnFocusToggle(object sender, RoutedEventArgs e) => _viewModel.ToggleFocus();

    private void OnFocusReset(object sender, RoutedEventArgs e) => _viewModel.ResetFocus();

    private void OnNextQuote(object sender, RoutedEventArgs e) => _viewModel.NextQuote();

    private void OnOpenSettings(object sender, RoutedEventArgs e) => ShowSettings();

    private void OnOpenModules(object sender, RoutedEventArgs e) => ShowSettings(openModulesTab: true);

    private void OnOpenNotes(object sender, RoutedEventArgs e) => ShowNotes();

    private void OnNewNote(object sender, RoutedEventArgs e) => ShowNotes(newNote: true);

    private void OnOpenClipboard(object sender, RoutedEventArgs e) => ShowClipboard();

    private void OnScreenTimeDetails(object sender, RoutedEventArgs e) => MessageBox.Show(
        this,
        "Screen time only records how long each application was in the foreground, in totals.\n\n" +
        "It never stores window titles, page addresses, file names or content, and the history " +
        "stays in %APPDATA%\\DeskDot\\screentime.json on this PC.",
        "About screen time",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    private void OnFocusCompleted(object? sender, EventArgs e) =>
        _tray?.ShowBalloon("Focus session complete ✨", "Nice work — take a short break.");

    /* ------------------------------------------------------------------ */
    /*  Dialogs                                                            */
    /* ------------------------------------------------------------------ */

    public void ShowSettings(bool openModulesTab = false)
    {
        ShowWindow();

        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            if (openModulesTab)
            {
                _settingsWindow.ShowModulesTab();
            }

            return;
        }

        _settingsWindow = new SettingsWindow(_settings.Clone());
        _settingsWindow.SettingsSaved += OnSettingsSaved;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Owner = this;
        _settingsWindow.Show();

        if (openModulesTab)
        {
            _settingsWindow.ShowModulesTab();
        }
    }

    private void ShowNotes(bool newNote = false)
    {
        var window = new NotesWindow(_viewModel) { Owner = this };
        window.ShowDialog();
    }

    private void ShowClipboard()
    {
        var window = new ClipboardWindow(_viewModel) { Owner = this };
        window.ShowDialog();
    }

    private void OnSettingsSaved(object? sender, AppSettings updated)
    {
        var previousToggle = _settings.ToggleHotkey;
        var previousSettings = _settings.SettingsHotkey;

        _settings.ToggleHotkey = updated.ToggleHotkey;
        _settings.SettingsHotkey = updated.SettingsHotkey;
        _settings.AlwaysOnTop = updated.AlwaysOnTop;
        _settings.StartWithWindows = updated.StartWithWindows;
        _settings.StatsIntervalMs = updated.StatsIntervalMs;
        _settings.FocusMinutes = updated.FocusMinutes;
        _settings.DisplayName = updated.DisplayName;
        _settings.WeatherEnabled = updated.WeatherEnabled;
        _settings.WeatherLocation = updated.WeatherLocation;
        _settings.WeatherLatitude = updated.WeatherLatitude;
        _settings.WeatherLongitude = updated.WeatherLongitude;
        _settings.WeatherUseFahrenheit = updated.WeatherUseFahrenheit;
        _settings.ClipboardHistoryEnabled = updated.ClipboardHistoryEnabled;
        _settings.ScreenTimeEnabled = updated.ScreenTimeEnabled;
        _settings.MaskSensitiveClipboard = updated.MaskSensitiveClipboard;
        _settings.EnabledModules = new List<string>(updated.EnabledModules);

        if (Topmost != _settings.AlwaysOnTop)
        {
            Topmost = _settings.AlwaysOnTop;
        }

        StartupService.Apply(_settings.StartWithWindows);

        if (!string.Equals(previousToggle, _settings.ToggleHotkey, StringComparison.OrdinalIgnoreCase))
        {
            _toggleHotkey?.Register(_settings.ToggleHotkey);
        }

        if (!string.Equals(previousSettings, _settings.SettingsHotkey, StringComparison.OrdinalIgnoreCase))
        {
            _settingsHotkey?.Register(_settings.SettingsHotkey);
        }

        _viewModel.ApplySettings(_settings);
        App.SaveSettings(_settings);
    }

    /* ------------------------------------------------------------------ */
    /*  Shutdown                                                           */
    /* ------------------------------------------------------------------ */

    public void RequestExit()
    {
        _allowClose = true;
        PersistGeometry();
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // Closing the window keeps DeskDot in the tray; only Exit really quits.
            e.Cancel = true;
            HideToTray();
            return;
        }

        _toggleHotkey?.Dispose();
        _settingsHotkey?.Dispose();
        _power?.Dispose();
        _tray?.Dispose();
        _viewModel.Dispose();

        App.SaveSettings(_settings);
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
