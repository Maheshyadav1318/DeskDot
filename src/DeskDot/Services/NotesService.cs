using System.IO;
using System.Text.Json;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>Quick notes, stored locally in <c>%APPDATA%\DeskDot\notes.json</c>.</summary>
public sealed class NotesService
{
    private readonly string _path;
    private readonly List<NoteEntry> _notes = new();

    public NotesService(string? directory = null)
    {
        var root = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskDot");
        _path = Path.Combine(root, "notes.json");
        Load();
    }

    public event EventHandler? Changed;

    public IReadOnlyList<NoteEntry> Notes => _notes;

    public NoteEntry? Latest => _notes.Count > 0 ? _notes[0] : null;

    public NoteEntry Add(string title, string body)
    {
        var note = NoteEntry.Create(
            string.IsNullOrWhiteSpace(title) ? FirstLineOf(body) : title.Trim(),
            body.Trim());

        _notes.Insert(0, note);
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
        return note;
    }

    public void Update(Guid id, string title, string body)
    {
        var index = _notes.FindIndex(note => note.Id == id);
        if (index < 0)
        {
            return;
        }

        _notes[index] = _notes[index] with
        {
            Title = string.IsNullOrWhiteSpace(title) ? FirstLineOf(body) : title.Trim(),
            Body = body.Trim(),
            UpdatedAt = DateTimeOffset.Now
        };

        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Delete(Guid id)
    {
        var removed = _notes.RemoveAll(note => note.Id == id) > 0;
        if (removed)
        {
            Save();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    private static string FirstLineOf(string body)
    {
        var line = body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(line) ? "Untitled note" : line[..Math.Min(line.Length, 60)];
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var stored = JsonSerializer.Deserialize<List<NoteEntry>>(File.ReadAllText(_path));
            if (stored is not null)
            {
                _notes.Clear();
                _notes.AddRange(stored.OrderByDescending(note => note.UpdatedAt));
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt notes file: start empty rather than failing to launch.
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
            var temp = _path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Notes are best effort; never interrupt the user.
        }
    }
}
