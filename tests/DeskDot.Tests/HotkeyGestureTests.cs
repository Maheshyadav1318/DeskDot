using DeskDot.Core;
using Xunit;

namespace DeskDot.Tests;

public class HotkeyGestureTests
{
    [Fact]
    public void Parses_default_gesture()
    {
        Assert.True(HotkeyGesture.TryParse("Alt+Space", out var modifiers, out var key));

        Assert.Equal(HotkeyGesture.ModAlt, modifiers);
        Assert.Equal(0x20u, key);
    }

    [Theory]
    [InlineData("Ctrl+Shift+D", HotkeyGesture.ModControl | HotkeyGesture.ModShift, (uint)'D')]
    [InlineData("win+alt+d", HotkeyGesture.ModWin | HotkeyGesture.ModAlt, (uint)'D')]
    [InlineData("Alt+F5", HotkeyGesture.ModAlt, 0x74u)]
    [InlineData("Ctrl+Escape", HotkeyGesture.ModControl, 0x1Bu)]
    public void Parses_modifier_combinations(string gesture, uint expectedMods, uint expectedKey)
    {
        Assert.True(HotkeyGesture.TryParse(gesture, out var modifiers, out var key));

        Assert.Equal(expectedMods, modifiers);
        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Space")]            // no modifier
    [InlineData("Alt")]              // no key
    [InlineData("Alt+Ctrl")]         // modifiers only
    [InlineData("Alt+Space+D")]      // two keys
    [InlineData("Alt+NotAKey")]
    public void Rejects_invalid_gestures(string? gesture)
    {
        Assert.False(HotkeyGesture.TryParse(gesture, out _, out _));
    }

    [Theory]
    [InlineData("Alt+Space")]
    [InlineData("Ctrl+Shift+D")]
    [InlineData("Win+Alt+F12")]
    [InlineData("Ctrl+Escape")]
    [InlineData("Alt+Enter")]
    public void Round_trips_through_Format(string gesture)
    {
        Assert.True(HotkeyGesture.TryParse(gesture, out var modifiers, out var key));

        var formatted = HotkeyGesture.Format(modifiers, key);

        Assert.True(HotkeyGesture.TryParse(formatted, out var modifiers2, out var key2));
        Assert.Equal(modifiers, modifiers2);
        Assert.Equal(key, key2);
    }

    [Fact]
    public void Format_uses_canonical_modifier_order()
    {
        var formatted = HotkeyGesture.Format(
            HotkeyGesture.ModWin | HotkeyGesture.ModShift | HotkeyGesture.ModControl | HotkeyGesture.ModAlt,
            0x20);

        Assert.Equal("Ctrl+Alt+Shift+Win+Space", formatted);
    }

    [Fact]
    public void Default_gesture_parses()
    {
        Assert.True(HotkeyGesture.TryParse(HotkeyGesture.Default, out _, out _));
    }
}
