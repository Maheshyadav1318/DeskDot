using System.Threading;
using System.Windows;
using DeskDot.Models;
using DeskDot.Services;

namespace DeskDot;

/// <summary>Application entry point: single instance, tray resident, settings aware.</summary>
public partial class App : Application
{
    private const string MutexName = @"Local\DeskDot.SingleInstance";
    private const string ShowEventName = @"Local\DeskDot.ShowIsland";

    private static SettingsService? _settingsService;
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showWait;
    private MainWindow? _mainWindow;

    public static AppSettings Settings { get; private set; } = new();

    public static void SaveSettings(AppSettings settings)
    {
        Settings = settings;
        _settingsService?.Save(settings);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalRunningInstance();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);

        _settingsService = new SettingsService();
        Settings = _settingsService.Load();

        _mainWindow = new MainWindow(Settings, new AgendaService(), new NotesService());
        _mainWindow.ExitRequested += (_, _) => Shutdown();
        MainWindow = _mainWindow;
        _mainWindow.Show();

        var startHidden = Settings.StartMinimised ||
                          e.Args.Any(arg => string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(arg, "--minimised", StringComparison.OrdinalIgnoreCase));
        if (startHidden)
        {
            _mainWindow.Hide();
        }

        if (e.Args.Any(arg => string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)))
        {
            _mainWindow.ShowSettings();
        }

        ListenForShowRequests();
    }

    private void ListenForShowRequests()
    {
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(
            _showEvent,
            (_, _) => Dispatcher.BeginInvoke(() => _mainWindow?.ToggleWindow()),
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    private static void SignalRunningInstance()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ShowEventName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or WaitHandleCannotBeOpenedException)
        {
            // Best effort: the second instance exits either way.
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.Handled = true; // keep the overlay alive through recoverable UI faults
    }

    private static void LogCrash(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeskDot");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:u}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Never let diagnostics take down the app.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showWait?.Unregister(null);
        _showWait = null;
        _showEvent?.Dispose();
        try
        {
            _instanceMutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Mutex was already released or never owned.
        }

        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
