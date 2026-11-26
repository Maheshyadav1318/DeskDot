using System.Runtime.InteropServices;
using System.Windows;
using DeskDot.Core;
using DeskDot.Models;
using DeskDot.ViewModels;

namespace DeskDot.Views;

/// <summary>Read-only browser over the in-memory clipboard history.</summary>
public partial class ClipboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;

    public ClipboardWindow(DashboardViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        var rows = _viewModel.ClipboardEntries
            .Select(entry => new ClipboardRow(
                entry.Text,
                entry.IsSensitive ? ClipboardText.MaskPreview(entry.Text) : ClipboardText.Preview(entry.Text, 220),
                ClipboardText.DescribeAge(DateTimeOffset.Now - entry.CapturedAt),
                entry.IsSensitive))
            .ToList();

        EntriesList.ItemsSource = null;
        EntriesList.ItemsSource = rows;
        EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCopyEntry(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string text })
        {
            return;
        }

        try
        {
            Clipboard.SetText(text);
        }
        catch (ExternalException)
        {
            // Another process holds the clipboard; nothing else to do.
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearClipboard();
        Refresh();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    /// <summary>Row shape for the list (public so the binding engine can read it).</summary>
    public sealed record ClipboardRow(string Text, string Display, string Age, bool IsSensitive);
}
