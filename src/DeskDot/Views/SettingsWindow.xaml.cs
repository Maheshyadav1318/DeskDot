using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using DeskDot.Models;

namespace DeskDot.Views;

/// <summary>
/// "Make yourself at home." — window behaviour, hotkeys, modules, weather and the two
/// privacy-sensitive modules. Saves a copy of the settings and raises
/// <see cref="SettingsSaved"/> with it; the caller persists and applies it.
/// </summary>
public partial class SettingsWindow : Window
{
    private static readonly string[] HotkeyChoices =
    {
        "Ctrl+Alt+D",
        "Alt+Space",
        "Ctrl+Space",
        "Ctrl+Alt+Space",
        "Ctrl+Shift+Space",
        "Win+Shift+D"
    };

    private static readonly string[] SettingsHotkeyChoices =
    {
        "Ctrl+OemComma",
        "Ctrl+Alt+S",
        "Ctrl+Shift+OemComma",
        "Alt+OemComma"
    };

    private static readonly int[] FocusChoices = { 15, 20, 25, 30, 45, 50, 60 };

    private readonly AppSettings _draft;
    private readonly List<ModuleToggle> _moduleToggles = new();

    // Guards the two-way mirror between the Privacy checkboxes and the module list, so
    // setting one from the other does not rebuild the list under the user's cursor.
    private bool _syncingConsent;

    public SettingsWindow(AppSettings draft)
    {
        _draft = draft ?? throw new ArgumentNullException(nameof(draft));

        InitializeComponent();
        PopulateChoices();
        LoadValues();
    }

    public event EventHandler<AppSettings>? SettingsSaved;

    /// <summary>Brings the Modules tab forward (used by the "Manage modules" buttons).</summary>
    public void ShowModulesTab() => Tabs.SelectedIndex = 1;

    private void PopulateChoices()
    {
        foreach (var choice in HotkeyChoices)
        {
            ToggleHotkeyBox.Items.Add(choice);
        }

        foreach (var choice in SettingsHotkeyChoices)
        {
            SettingsHotkeyBox.Items.Add(choice);
        }

        foreach (var minutes in FocusChoices)
        {
            FocusLengthBox.Items.Add($"{minutes} minutes");
        }
    }

    private void LoadValues()
    {
        AlwaysOnTopCheck.IsChecked = _draft.AlwaysOnTop;
        StartMinimisedCheck.IsChecked = _draft.StartMinimised;
        StartWithWindowsCheck.IsChecked = _draft.StartWithWindows;
        DisplayNameBox.Text = _draft.DisplayName;

        EnsureSelected(ToggleHotkeyBox, _draft.ToggleHotkey);
        EnsureSelected(SettingsHotkeyBox, _draft.SettingsHotkey);

        FocusLengthBox.SelectedItem = $"{_draft.FocusMinutes} minutes";
        if (FocusLengthBox.SelectedItem is null)
        {
            EnsureSelected(FocusLengthBox, "25 minutes");
        }

        WeatherEnabledCheck.IsChecked = _draft.WeatherEnabled;
        WeatherLocationBox.Text = _draft.WeatherLocation;
        FahrenheitCheck.IsChecked = _draft.WeatherUseFahrenheit;

        ClipboardCheck.IsChecked = _draft.ClipboardHistoryEnabled;
        MaskSensitiveCheck.IsChecked = _draft.MaskSensitiveClipboard;
        ScreenTimeCheck.IsChecked = _draft.ScreenTimeEnabled;

        // The module list reads the consent flags, and the checkboxes are the same switch.
        ClipboardCheck.Checked += OnPrivacyConsentChanged;
        ClipboardCheck.Unchecked += OnPrivacyConsentChanged;
        ScreenTimeCheck.Checked += OnPrivacyConsentChanged;
        ScreenTimeCheck.Unchecked += OnPrivacyConsentChanged;

        BuildModuleList();
    }

    /// <summary>Mirrors a consent checkbox change into the module list immediately.</summary>
    private void OnPrivacyConsentChanged(object sender, RoutedEventArgs e)
    {
        _draft.ClipboardHistoryEnabled = ClipboardCheck.IsChecked == true;
        _draft.ScreenTimeEnabled = ScreenTimeCheck.IsChecked == true;

        if (_syncingConsent)
        {
            return;
        }

        _syncingConsent = true;
        try
        {
            BuildModuleList();
        }
        finally
        {
            _syncingConsent = false;
        }
    }

    private static void EnsureSelected(ComboBox box, string value)
    {
        if (!box.Items.Contains(value))
        {
            box.Items.Add(value);
        }

        box.SelectedItem = value;
    }

    private void BuildModuleList()
    {
        var enabled = _draft.EnabledModules.Count > 0
            ? _draft.EnabledModules.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : ModuleInfo.Defaults.Select(module => module.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        _moduleToggles.Clear();
        foreach (var info in ModuleInfo.All)
        {
            // The two private modules follow their consent checkboxes on the Privacy tab.
            var isEnabled = info.Id switch
            {
                DashboardModule.Clipboard => _draft.ClipboardHistoryEnabled,
                DashboardModule.ScreenTime => _draft.ScreenTimeEnabled,
                _ => enabled.Contains(info.Key)
            };

            var toggle = new ModuleToggle(info, isEnabled);
            toggle.Toggled += OnModuleToggled;
            _moduleToggles.Add(toggle);
        }

        ModulesList.ItemsSource = null;
        ModulesList.ItemsSource = _moduleToggles;
    }

    /// <summary>Keeps the Privacy tab honest when a private module is switched in the list.</summary>
    private void OnModuleToggled(object? sender, EventArgs e)
    {
        if (sender is not ModuleToggle toggle)
        {
            return;
        }

        _syncingConsent = true;
        try
        {
            switch (toggle.Info.Id)
            {
                case DashboardModule.Clipboard:
                    ClipboardCheck.IsChecked = toggle.IsEnabled;
                    _draft.ClipboardHistoryEnabled = toggle.IsEnabled;
                    break;
                case DashboardModule.ScreenTime:
                    ScreenTimeCheck.IsChecked = toggle.IsEnabled;
                    _draft.ScreenTimeEnabled = toggle.IsEnabled;
                    break;
            }
        }
        finally
        {
            _syncingConsent = false;
        }
    }

    private void OnLookUpWeather(object sender, RoutedEventArgs e) =>
        _ = LookUpAsync(WeatherLocationBox.Text);

    private async Task LookUpAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            WeatherStatusText.Text = "Type a city first, e.g. \"Guntur\" or \"Portland, OR\".";
            return;
        }

        WeatherStatusText.Text = "Looking up…";

        try
        {
            // Open-Meteo geocoding: the same provider as the forecast, so the user only ever
            // talks to one service. Only the typed city name is sent.
            var url = "https://geocoding-api.open-meteo.com/v1/search?count=1&language=en&format=json&name=" +
                      Uri.EscapeDataString(city);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await http.GetFromJsonAsync<GeocodeResponse>(url);

            var match = response?.Results?.FirstOrDefault();
            if (match is null)
            {
                WeatherStatusText.Text = "Couldn't find that place. Try adding a region, e.g. \"Guntur, India\".";
                return;
            }

            _draft.WeatherLatitude = match.Latitude;
            _draft.WeatherLongitude = match.Longitude;

            var label = string.Join(", ",
                new[] { match.Name, match.Admin1, match.Country }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Take(3));

            WeatherLocationBox.Text = label;
            WeatherStatusText.Text = $"Found {label} ({match.Latitude:0.##}, {match.Longitude:0.##}). Save to apply.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            WeatherStatusText.Text = "Couldn't reach the location service. Check your connection and try again.";
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _draft.AlwaysOnTop = AlwaysOnTopCheck.IsChecked == true;
        _draft.StartMinimised = StartMinimisedCheck.IsChecked == true;
        _draft.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _draft.DisplayName = DisplayNameBox.Text.Trim();

        if (ToggleHotkeyBox.SelectedItem is string toggle)
        {
            _draft.ToggleHotkey = toggle;
        }

        if (SettingsHotkeyBox.SelectedItem is string settingsHotkey)
        {
            _draft.SettingsHotkey = settingsHotkey;
        }

        if (FocusLengthBox.SelectedItem is string focus && int.TryParse(focus.Split(' ')[0], out var minutes))
        {
            _draft.FocusMinutes = minutes;
        }

        _draft.WeatherEnabled = WeatherEnabledCheck.IsChecked == true;
        _draft.WeatherLocation = string.IsNullOrWhiteSpace(WeatherLocationBox.Text)
            ? "Portland, OR"
            : WeatherLocationBox.Text.Trim();
        _draft.WeatherUseFahrenheit = FahrenheitCheck.IsChecked == true;

        if (_draft.WeatherLatitude is null || _draft.WeatherLongitude is null)
        {
            // Keeping the previous coordinates would point the card at the wrong city.
            WeatherStatusText.Text = "Pick a city with \"Look up\" so the forecast location is known.";
            if (_draft.WeatherEnabled)
            {
                InlineStatus.Text = "Weather needs a location — press Look up next to the city.";
                Tabs.SelectedIndex = 2;
                return;
            }
        }

        _draft.ClipboardHistoryEnabled = ClipboardCheck.IsChecked == true;
        _draft.MaskSensitiveClipboard = MaskSensitiveCheck.IsChecked == true;
        _draft.ScreenTimeEnabled = ScreenTimeCheck.IsChecked == true;

        _draft.EnabledModules = _moduleToggles
            .Where(toggle => toggle.IsEnabled)
            .Select(toggle => toggle.Info.Key)
            .ToList();

        SettingsSaved?.Invoke(this, _draft);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    /// <summary>Bindable row for the module list.</summary>
    public sealed class ModuleToggle : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isEnabled;

        public ModuleToggle(ModuleInfo info, bool isEnabled)
        {
            Info = info;
            _isEnabled = isEnabled;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public ModuleInfo Info { get; }

        public string Name => Info.Name;

        public string Description => Info.Description;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
                Toggled?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Lets the window mirror a private module back to its consent checkbox.</summary>
        public event EventHandler? Toggled;
    }

    private sealed class GeocodeResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodeResult>? Results { get; set; }
    }

    private sealed class GeocodeResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("admin1")]
        public string? Admin1 { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
