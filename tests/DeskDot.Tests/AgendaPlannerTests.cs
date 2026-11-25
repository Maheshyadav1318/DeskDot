using DeskDot.Core;
using DeskDot.Models;
using Xunit;

namespace DeskDot.Tests;

public class AgendaPlannerTests
{
    [Fact]
    public void Parse_reads_time_and_title()
    {
        var items = AgendaPlanner.Parse(new[] { "09:30 Stand-up & inbox triage" });

        var item = Assert.Single(items);
        Assert.Equal("09:30", item.Time);
        Assert.Equal("Stand-up & inbox triage", item.Title);
    }

    [Fact]
    public void Parse_skips_blank_comments_and_unparseable_lines()
    {
        var items = AgendaPlanner.Parse(new[]
        {
            "",
            "   ",
            "# a comment",
            "no time here",
            "09:30",
            "25:99 Not a real time",
            "11:00 Real item"
        });

        var item = Assert.Single(items);
        Assert.Equal("Real item", item.Title);
    }

    [Fact]
    public void Parse_accepts_single_digit_hour()
    {
        var items = AgendaPlanner.Parse(new[] { "9:05 Quick sync" });

        Assert.Equal("9:05", Assert.Single(items).Time);
    }

    [Fact]
    public void Next_returns_first_item_when_day_has_not_started()
    {
        var items = AgendaPlanner.Parse(new[] { "14:00 Later", "09:30 Earlier" });

        var next = AgendaPlanner.Next(items, new TimeOnly(8, 0));

        Assert.Equal("Earlier", next.Title); // unsorted input still picks the earliest
    }

    [Fact]
    public void Next_returns_item_at_or_after_now()
    {
        var items = AgendaPlanner.Parse(new[] { "09:30 A", "11:00 B", "16:30 C" });

        Assert.Equal("B", AgendaPlanner.Next(items, new TimeOnly(11, 0)).Title);
        Assert.Equal("C", AgendaPlanner.Next(items, new TimeOnly(11, 1)).Title);
    }

    [Fact]
    public void Next_wraps_to_first_item_when_day_is_over()
    {
        var items = AgendaPlanner.Parse(new[] { "09:30 A", "11:00 B" });

        Assert.Equal("A", AgendaPlanner.Next(items, new TimeOnly(23, 59)).Title);
    }

    [Fact]
    public void Next_returns_none_for_empty_list()
    {
        Assert.Same(AgendaItem.None, AgendaPlanner.Next(Array.Empty<AgendaItem>(), new TimeOnly(9, 0)));
    }

    [Theory]
    [InlineData("09:30", 9, 30)]
    [InlineData("9:30", 9, 30)]
    [InlineData("2026-11-25 14:00", 14, 0)]
    public void ParseTime_handles_supported_formats(string text, int hours, int minutes)
    {
        var time = AgendaPlanner.ParseTime(text);

        Assert.NotNull(time);
        Assert.Equal(new TimeSpan(hours, minutes, 0), time!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("half past nine")]
    [InlineData("99:99")]
    public void ParseTime_rejects_garbage(string text)
    {
        Assert.Null(AgendaPlanner.ParseTime(text));
    }

    [Fact]
    public void DemoAgenda_is_not_empty_and_ordered()
    {
        Assert.NotEmpty(AgendaPlanner.DemoAgenda);
        var times = AgendaPlanner.DemoAgenda.Select(i => AgendaPlanner.ParseTime(i.Time)).ToList();
        Assert.All(times, t => Assert.NotNull(t));
        Assert.Equal(times.OrderBy(t => t), times);
    }
}
