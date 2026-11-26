using System.Windows;
using DeskDot.ViewModels;

namespace DeskDot.Views;

/// <summary>Notes browser and composer. Notes are local to this PC.</summary>
public partial class NotesWindow : Window
{
    private readonly DashboardViewModel _viewModel;

    public NotesWindow(DashboardViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        Refresh();
        Loaded += (_, _) => BodyBox.Focus();
    }

    private void Refresh()
    {
        var notes = _viewModel.AllNotes.ToList();
        NotesList.ItemsSource = null;
        NotesList.ItemsSource = notes;
        EmptyText.Visibility = notes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSaveNote(object sender, RoutedEventArgs e)
    {
        var body = BodyBox.Text;
        if (string.IsNullOrWhiteSpace(body))
        {
            StatusText.Text = "Write something first.";
            return;
        }

        _viewModel.AddNote(string.Empty, body);
        BodyBox.Clear();
        StatusText.Text = "Saved.";
        Refresh();
    }

    private void OnDeleteNote(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            _viewModel.DeleteNote(id);
            Refresh();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
