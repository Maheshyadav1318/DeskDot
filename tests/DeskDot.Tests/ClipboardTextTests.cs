using DeskDot.Core;
using Xunit;

namespace DeskDot.Tests;

public class ClipboardTextTests
{
    [Theory]
    [InlineData("https://github.com", "url")]
    [InlineData("HTTP://example.com", "url")]
    [InlineData("www.example.com", "url")]
    [InlineData("hello@example.com", "email")]
    [InlineData(@"C:\Users\me\notes.txt", "path")]
    [InlineData("just a sentence", "text")]
    [InlineData("", "empty")]
    public void Classify_recognises_common_shapes(string text, string expected)
    {
        Assert.Equal(expected, ClipboardText.Classify(text));
    }

    [Fact]
    public void Preview_flattens_whitespace_and_clips()
    {
        var preview = ClipboardText.Preview("line one\n\n  line two\ttabbed", 100);

        Assert.Equal("line one line two tabbed", preview);
        Assert.DoesNotContain("\n", preview);
    }

    [Fact]
    public void Preview_appends_ellipsis_when_clipped()
    {
        var preview = ClipboardText.Preview(new string('a', 200), 20);

        Assert.Equal(20, preview.Length);
        Assert.EndsWith("…", preview);
    }

    [Fact]
    public void Preview_handles_empty_input()
    {
        Assert.Equal(string.Empty, ClipboardText.Preview(null));
        Assert.Equal(string.Empty, ClipboardText.Preview("   "));
    }

    [Theory]
    [InlineData("sk-live-9f8a7b6c5d4e3f2a1b0c9d8e", true)]   // token-like: long, mixed, symbols
    [InlineData("aB3xY7zQ9wE1rT5uI8oP2aS4dF6gH0jK", true)]      // long alphanumeric blob
    [InlineData("correct horse battery staple", false)]         // passphrase with spaces
    [InlineData("hello world", false)]                          // short
    [InlineData("", false)]
    public void LooksSensitive_flags_dense_mixed_strings(string text, bool expected)
    {
        Assert.Equal(expected, ClipboardText.LooksSensitive(text));
    }

    [Fact]
    public void MaskPreview_hides_the_content_and_is_bounded()
    {
        var masked = ClipboardText.MaskPreview("super-secret-token-value");

        Assert.DoesNotContain("secret", masked);
        Assert.All(masked, c => Assert.Equal('•', c));
        Assert.InRange(masked.Length, 8, 24);
    }

    [Theory]
    [InlineData(10, "just now")]
    [InlineData(120, "2 min ago")]
    [InlineData(3700, "1 h ago")]
    [InlineData(90_000, "1 d ago")]
    public void DescribeAge_reads_naturally(int seconds, string expected)
    {
        Assert.Equal(expected, ClipboardText.DescribeAge(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Trim_drops_blanks_and_consecutive_duplicates()
    {
        var trimmed = ClipboardText.Trim(new[] { "a", "a", "", "b", "  ", "b" });

        Assert.Equal(new[] { "a", "b" }, trimmed);
    }

    [Fact]
    public void Trim_caps_the_history()
    {
        var trimmed = ClipboardText.Trim(Enumerable.Range(0, 50).Select(index => $"entry {index}"), 25);

        Assert.Equal(25, trimmed.Count);
        Assert.Equal("entry 0", trimmed[0]);
    }

    [Fact]
    public void Trim_truncates_very_large_entries()
    {
        var huge = new string('x', ClipboardText.MaxStoredLength + 500);

        Assert.Equal(ClipboardText.MaxStoredLength, ClipboardText.Trim(new[] { huge }).Single().Length);
    }

    [Theory]
    [InlineData(0, "No items")]
    [InlineData(1, "1 item")]
    [InlineData(4, "4 items")]
    public void CountLabel_is_grammatical(int count, string expected)
    {
        Assert.Equal(expected, ClipboardText.CountLabel(count));
    }
}
