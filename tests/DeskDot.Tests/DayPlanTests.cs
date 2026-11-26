using DeskDot.Core;
using Xunit;

namespace DeskDot.Tests;

public class DayPlanTests
{
    [Fact]
    public void Parse_reads_time_title_and_optional_detail()
    {
        var entries = DayPlan.Parse(new[]
        {
            "11:30 Design catch-up | 30 min · Product design",
            "14:00 A little focus time"
        });

        Assert.Equal(2, entries.Count);
        Assert.Equal("11:30", entries[0].Time);
        Assert.Equal("Design catch-up", entries[0].Title);
        Assert.Equal("30 min · Product design", entries[0].Detail);
        Assert.Null(entries[1].Detail);
    }

    [Fact]
    public void Parse_skips_comments_blank_and_invalid_lines()
    {
        var entries = DayPlan.Parse(new[] { "", "   ", "# a comment", "no time", "25:99 late", "09:00 Real" });

        Assert.Single(entries);
        Assert.Equal("Real", entries[0].Title);
    }

    [Fact]
    public void Parse_accepts_am_pm_times_and_normalises_them()
    {
        var entries = DayPlan.Parse(new[] { "11:30AM Lunch", "14:00PM Focus" });

        Assert.Equal("11:30", entries[0].Time);
        Assert.Equal("14:00", entries[1].Time); // 14:00PM is 14:00 (12-hour reduced then AM/PM applied)
    }

    [Fact]
    public void Parse_keeps_a_detail_with_pipes_inside()
    {
        var entries = DayPlan.Parse(new[] { "09:00 Sync | notes | follow ups" });

        Assert.Equal("notes | follow ups", entries[0].Detail);
    }

    [Fact]
    public void Split_separates_upcoming_from_earlier()
    {
        var entries = DayPlan.Parse(new[] { "09:00 Early", "11:30 Mid", "14:00 Late" });

        var (upcoming, earlier) = DayPlan.Split(entries, new TimeOnly(12, 0));

        Assert.Single(earlier);
        Assert.Equal("Early", earlier[0].Title);
        Assert.Equal(2, upcoming.Count);
        Assert.Equal("Mid", upcoming[0].Title);
    }

    [Fact]
    public void Split_treats_an_exact_time_as_upcoming()
    {
        var entries = DayPlan.Parse(new[] { "12:00 Now" });

        var (upcoming, earlier) = DayPlan.Split(entries, new TimeOnly(12, 0));

        Assert.Single(upcoming);
        Assert.Empty(earlier);
    }

    [Fact]
    public void Next_returns_none_once_the_day_is_done()
    {
        var entries = DayPlan.Parse(new[] { "09:00 Early" });

        Assert.Same(PlanEntry.None, DayPlan.Next(entries, new TimeOnly(23, 0)));
    }

    [Fact]
    public void RemainingToday_counts_only_the_future()
    {
        var entries = DayPlan.Parse(new[] { "09:00 A", "11:00 B", "16:00 C" });

        Assert.Equal(2, DayPlan.RemainingToday(entries, new TimeOnly(10, 0)));
        Assert.Equal(0, DayPlan.RemainingToday(entries, new TimeOnly(20, 0)));
    }

    [Theory]
    [InlineData("11:30", "11:30AM")]
    [InlineData("00:05", "12:05AM")]
    [InlineData("12:00", "12:00PM")]
    [InlineData("23:59", "11:59PM")]
    public void FormatClock_converts_to_the_card_style(string input, string expected)
    {
        Assert.Equal(expected, DayPlan.FormatClock(input));
    }

    [Theory]
    [InlineData("9:05", 9, 5)]
    [InlineData("14:00", 14, 0)]
    [InlineData("11:30AM", 11, 30)]
    [InlineData("12:00PM", 12, 0)]
    [InlineData("12:30AM", 0, 30)]
    public void ParseClock_accepts_24_and_12_hour_forms(string text, int hours, int minutes)
    {
        var parsed = DayPlan.ParseClock(text);

        Assert.NotNull(parsed);
        Assert.Equal(new TimeSpan(hours, minutes, 0), parsed!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("later")]
    [InlineData("99:99")]
    public void ParseClock_rejects_garbage(string text)
    {
        Assert.Null(DayPlan.ParseClock(text));
    }

    [Fact]
    public void Sample_plan_is_ordered_and_parseable()
    {
        Assert.NotEmpty(DayPlan.Sample);
        var times = DayPlan.Sample.Select(entry => DayPlan.ParseClock(entry.Time)).ToList();
        Assert.All(times, time => Assert.NotNull(time));
        Assert.Equal(times.OrderBy(time => time), times);
    }
}
