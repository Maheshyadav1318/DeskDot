using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under <c>%APPDATA%\DeskDot</c>.
/// Writes are atomic (temp file + replace) and failures never propagate: a damaged or
/// read-only settings file must not stop the overlay from starting.
/// </summary>
public sealed class SettingsService
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _directory;
    private readonly string _path;

    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskDot"))
    {
    }

    /// <summary>Test seam: store settings in any directory.</summary>
    public SettingsService(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _path = Path.Combine(_directory, "settings.json");
    }

    public string FilePath => _path;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public bool Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            Directory.CreateDirectory(_directory);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);

            // Write-then-replace so a crash mid-save cannot truncate the file.
            var temp = _path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
