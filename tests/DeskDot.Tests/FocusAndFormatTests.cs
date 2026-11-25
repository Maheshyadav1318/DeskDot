using DeskDot.Core;
using DeskDot.Models;
using Xunit;

namespace DeskDot.Tests;

public class FocusClockTests
{
    [Theory]
    [InlineData(25 * 60 + 0, "25:00")]
    [InlineData(9 * 60 + 5, "09:05")]
    [InlineData(60, "01:00")]
    [InlineData(59, "00:59")]
    [InlineData(0, "00:00")]
    [InlineData(-30, "00:00")]
    public void Format_produces_mm_ss(int seconds, string expected)
    {
        Assert.Equal(expected, FocusClock.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Format_caps_minutes_at_99()
    {
        Assert.Equal("99:00", FocusClock.Format(TimeSpan.FromMinutes(180)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(25, 25)]
    [InlineData(500, 180)]
    public void ClampDuration_bounds_the_session(int minutes, int expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), FocusClock.ClampDuration(minutes));
    }

    [Theory]
    [InlineData(25, 25, 100)]
    [InlineData(0, 25, 0)]
    [InlineData(12.5, 25, 50)]
    [InlineData(30, 25, 100)]
    public void RemainingPercent_is_clamped(double remainingMinutes, double totalMinutes, double expected)
    {
        var percent = FocusClock.RemainingPercent(
            TimeSpan.FromMinutes(remainingMinutes),
            TimeSpan.FromMinutes(totalMinutes));

        Assert.Equal(expected, percent, 3);
    }

    [Fact]
    public void RemainingPercent_is_zero_for_zero_total()
    {
        Assert.Equal(0, FocusClock.RemainingPercent(TimeSpan.FromMinutes(5), TimeSpan.Zero));
    }
}

public class SystemFormatTests
{
    [Theory]
    [InlineData(0, "0%")]
    [InlineData(14.4, "14%")]
    [InlineData(99.6, "100%")]
    [InlineData(-5, "0%")]
    [InlineData(140, "100%")]
    public void Cpu_is_clamped_and_rounded(double percent, string expected)
    {
        Assert.Equal(expected, SystemFormat.Cpu(percent));
    }

    [Theory]
    [InlineData(11, "11G")]
    [InlineData(10, "10G")]
    [InlineData(8.42, "8.4G")]
    [InlineData(0, "0.0G")]
    [InlineData(-3, "0.0G")]
    public void Memory_switches_precision_at_ten_gigabytes(double usedGb, string expected)
    {
        Assert.Equal(expected, SystemFormat.Memory(usedGb));
    }

    [Fact]
    public void MemorySummary_includes_total()
    {
        Assert.Equal("8.0 / 16G", SystemFormat.MemorySummary(new SystemSnapshot(10, 8, 16)));
    }

    [Fact]
    public void MemorySummary_is_dash_without_total()
    {
        Assert.Equal("—", SystemFormat.MemorySummary(SystemSnapshot.Empty));
    }

    [Theory]
    [InlineData(8, 16, 50)]
    [InlineData(0, 0, 0)]
    [InlineData(20, 16, 100)]
    public void MemoryPercent_is_safe_and_clamped(double used, double total, double expected)
    {
        Assert.Equal(expected, SystemFormat.MemoryPercent(used, total), 3);
    }
}

public class GreetingRulesTests
{
    [Theory]
    [InlineData(2, "Still up? Take it easy. 🌙")]
    [InlineData(7, "Good morning! ✨")]
    [InlineData(14, "Have a wonderful day! ✨")]
    [InlineData(19, "Good evening! 🌇")]
    [InlineData(23, "Winding down. 🌙")]
    [InlineData(-1, "Have a wonderful day! ✨")]
    public void ForHour_picks_the_right_sentence(int hour, string expected)
    {
        Assert.Equal(expected, GreetingRules.ForHour(hour));
    }

    [Fact]
    public void ForHour_folds_in_the_display_name()
    {
        Assert.Equal("Good morning, Mahesh! ✨", GreetingRules.ForHour(8, "Mahesh"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForHour_ignores_a_blank_name(string? name)
    {
        Assert.Equal("Good morning! ✨", GreetingRules.ForHour(8, name));
    }
}
