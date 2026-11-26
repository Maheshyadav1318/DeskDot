using System.Globalization;

namespace DeskDot.Core;

/// <summary>What a clipboard entry looks like on the card.</summary>
public sealed record ClipboardPreview(string Text, string Kind, string Age);

/// <summary>
/// Pure helpers for clipboard history: classifying an entry, trimming it for display and
/// trimming the history list. Sensitive-looking entries are flagged so the UI can mask them.
/// </summary>
public static class ClipboardText
{
    public const int MaxEntries = 25;
    private const int PreviewLength = 90;

    /// <summary>Longest text we keep in memory; anything bigger is truncated on capture.</summary>
    public const int MaxStoredLength = 20_000;

    /// <summary>Classifies an entry as url / email / path / number / text.</summary>
    public static string Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "empty";
        }

        var trimmed = text.Trim();

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return "url";
        }

        if (!trimmed.Contains(' ') && trimmed.Contains('@') && trimmed.Contains('.'))
        {
            return "email";
        }

        if (trimmed.Contains('\\') || (trimmed.Length > 2 && trimmed[1] == ':'))
        {
            return "path";
        }

        return "text";
    }

    /// <summary>
    /// Flattens whitespace and clips to a preview length, appending an ellipsis when clipped.
    /// </summary>
    public static string Preview(string? text, int maxLength = PreviewLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var flattened = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flattened.Length <= maxLength
            ? flattened
            : flattened[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
    }

    /// <summary>
    /// Heuristic: does this look like a secret (password manager output, token, card number)?
    /// Used to mask the preview, never to block capture.
    /// </summary>
    public static bool LooksSensitive(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.Length < 12)
        {
            return false;
        }

        var hasLetter = trimmed.Any(char.IsLetter);
        var hasDigit = trimmed.Any(char.IsDigit);
        var hasSymbol = trimmed.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

        // Long, dense, mixed-class strings are usually keys/tokens.
        return hasLetter && hasDigit && (hasSymbol || trimmed.Length >= 24) && !trimmed.Contains(' ');
    }

    /// <summary>Masks a preview for display when an entry looks sensitive.</summary>
    public static string MaskPreview(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : new string('•', Math.Min(24, Math.Max(8, text.Length)));

    /// <summary>"2 min ago" / "just now" / "3 h ago".</summary>
    public static string DescribeAge(TimeSpan age)
    {
        if (age < TimeSpan.FromSeconds(45))
        {
            return "just now";
        }

        if (age < TimeSpan.FromMinutes(60))
        {
            var minutes = Math.Max(1, (int)age.TotalMinutes);
            return $"{minutes} min ago";
        }

        if (age < TimeSpan.FromHours(24))
        {
            var hours = Math.Max(1, (int)age.TotalHours);
            return $"{hours} h ago";
        }

        var days = Math.Max(1, (int)age.TotalDays);
        return $"{days} d ago";
    }

    /// <summary>
    /// Drops the oldest entries until the list fits, and de-duplicates consecutive repeats
    /// (copying the same thing twice should not create two rows).
    /// </summary>
    public static IReadOnlyList<string> Trim(IEnumerable<string> entries, int max = MaxEntries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var result = new List<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            if (result.Count > 0 && string.Equals(result[^1], entry, StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(entry.Length > MaxStoredLength ? entry[..MaxStoredLength] : entry);
        }

        return result.Count <= max ? result : result.Take(max).ToList();
    }

    /// <summary>Formats a count for the "N items" footer.</summary>
    public static string CountLabel(int count) => count switch
    {
        <= 0 => "No items",
        1 => "1 item",
        _ => string.Create(CultureInfo.InvariantCulture, $"{count} items")
    };
}
