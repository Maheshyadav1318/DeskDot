using System.Windows;
using DeskDot.Models;

namespace DeskDot.Views;

/// <summary>Small setup dialog mirroring the "DeskDot Setup" surface in the design.</summary>
public partial class SettingsWindow : Window
{
    private static readonly string[] HotkeyChoices =
    {
        "Alt+Space",
        "Ctrl+Space",
        "Ctrl+Alt+Space",
        "Win+Shift+D",
        "Alt+Shift+D"
    };

    private static readonly int[] FocusChoices = { 15, 20, 25, 30, 45, 50 };

    private readonly AppSettings _draft;

    public SettingsWindow(AppSettings draft)
    {
        _draft = draft ?? throw new ArgumentNullException(nameof(draft));
        InitializeComponent();

        foreach (var choice in HotkeyChoices)
        {
            HotkeyBox.Items.Add(choice);
        }

        foreach (var minutes in FocusChoices)
        {
            FocusLengthBox.Items.Add($"{minutes}");
        }

        AlwaysOnTopCheck.IsChecked = _draft.AlwaysOnTop;
        CollapseOnFocusLossCheck.IsChecked = _draft.CollapseOnFocusLoss;
        StartWithWindowsCheck.IsChecked = _draft.StartWithWindows;
        DisplayNameBox.Text = _draft.DisplayName;

        if (!HotkeyBox.Items.Contains(_draft.ToggleHotkey))
        {
            HotkeyBox.Items.Add(_draft.ToggleHotkey);
        }

        HotkeyBox.SelectedItem = _draft.ToggleHotkey;
        FocusLengthBox.SelectedItem = $"{_draft.FocusMinutes}";
        if (FocusLengthBox.SelectedItem is null)
        {
            FocusLengthBox.SelectedIndex = FocusChoices.Length - 3; // 25 minutes
        }
    }

    public event EventHandler<AppSettings>? SettingsSaved;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _draft.AlwaysOnTop = AlwaysOnTopCheck.IsChecked == true;
        _draft.CollapseOnFocusLoss = CollapseOnFocusLossCheck.IsChecked == true;
        _draft.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _draft.DisplayName = DisplayNameBox.Text.Trim();

        if (HotkeyBox.SelectedItem is string hotkey)
        {
            _draft.ToggleHotkey = hotkey;
        }

        if (FocusLengthBox.SelectedItem is string minutes && int.TryParse(minutes, out var parsed))
        {
            _draft.FocusMinutes = parsed;
        }

        App.SaveSettings(_draft);
        SettingsSaved?.Invoke(this, _draft);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
