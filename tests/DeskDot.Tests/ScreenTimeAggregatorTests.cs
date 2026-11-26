using DeskDot.Core;
using Xunit;

namespace DeskDot.Tests;

public class ScreenTimeAggregatorTests
{
    private static ScreenTimeAggregator.Sample S(string process, double minutes) =>
        new(process, TimeSpan.FromMinutes(minutes));

    [Fact]
    public void Summarise_sums_repeats_and_orders_descending()
    {
        var totals = ScreenTimeAggregator.Summarise(new[]
        {
            S("chrome", 20),
            S("code", 30),
            S("chrome", 10)
        });

        Assert.Equal("chrome", totals[0].ProcessName);
        Assert.Equal(TimeSpan.FromMinutes(30), totals[0].Duration);
        Assert.Equal("code", totals[1].ProcessName);
    }

    [Fact]
    public void Summarise_percentages_add_up_to_100()
    {
        var totals = ScreenTimeAggregator.Summarise(new[] { S("a", 30), S("b", 10) });

        Assert.Equal(100, totals.Sum(t => t.Percent), 1);
        Assert.Equal(75, totals[0].Percent, 1);
    }

    [Fact]
    public void Summarise_drops_sub_minute_noise()
    {
        var totals = ScreenTimeAggregator.Summarise(new[] { S("a", 10), S("blip", 0.2) });

        Assert.Single(totals);
        Assert.Equal("a", totals[0].ProcessName);
    }

    [Fact]
    public void Summarise_keeps_the_largest_when_everything_is_tiny()
    {
        var totals = ScreenTimeAggregator.Summarise(new[] { S("a", 0.3), S("b", 0.1) });

        Assert.Single(totals);
        Assert.Equal("a", totals[0].ProcessName);
    }

    [Fact]
    public void Summarise_ignores_empty_process_names_and_zero_durations()
    {
        var totals = ScreenTimeAggregator.Summarise(new[]
        {
            new ScreenTimeAggregator.Sample("", TimeSpan.FromMinutes(5)),
            S("a", 0),
            S("b", 5)
        });

        Assert.Single(totals);
        Assert.Equal("b", totals[0].ProcessName);
    }

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(45, "45s")]
    [InlineData(60, "1m")]
    [InlineData(48 * 60, "48m")]
    [InlineData(3600, "1h")]
    [InlineData(2 * 3600 + 34 * 60, "2h 34m")]
    public void FormatDuration_reads_naturally(int seconds, string expected)
    {
        Assert.Equal(expected, ScreenTimeAggregator.FormatDuration(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void FormatDuration_clamps_negatives()
    {
        Assert.Equal("0s", ScreenTimeAggregator.FormatDuration(TimeSpan.FromMinutes(-5)));
    }

    [Fact]
    public void TrendPercent_is_null_without_yesterday()
    {
        Assert.Null(ScreenTimeAggregator.TrendPercent(TimeSpan.FromHours(2), TimeSpan.Zero));
    }

    [Theory]
    [InlineData(120, 120, 0)]
    [InlineData(90, 120, -25)]
    [InlineData(180, 120, 50)]
    public void TrendPercent_compares_against_yesterday(double todayMin, double yesterdayMin, double expected)
    {
        var trend = ScreenTimeAggregator.TrendPercent(
            TimeSpan.FromMinutes(todayMin),
            TimeSpan.FromMinutes(yesterdayMin));

        Assert.Equal(expected, trend!.Value, 1);
    }

    [Fact]
    public void Encouragement_is_gentle_and_never_judgemental()
    {
        var less = ScreenTimeAggregator.Encouragement(TimeSpan.FromHours(1), TimeSpan.FromHours(3));
        var same = ScreenTimeAggregator.Encouragement(TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        var more = ScreenTimeAggregator.Encouragement(TimeSpan.FromHours(6), TimeSpan.FromHours(2));

        Assert.Contains("less than yesterday", less);
        Assert.Contains("same as yesterday", same);
        Assert.Contains("break", more);
    }

    [Fact]
    public void WeekBars_scales_to_the_peak_and_pads_to_seven()
    {
        var bars = ScreenTimeAggregator.WeekBars(new[]
        {
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(4)
        });

        Assert.Equal(7, bars.Count);
        Assert.Equal(1, bars[^1], 3);
        Assert.All(bars, bar => Assert.InRange(bar, 0, 1));
    }

    [Fact]
    public void WeekBars_is_all_zero_for_an_empty_week()
    {
        Assert.All(ScreenTimeAggregator.WeekBars(Array.Empty<TimeSpan>()), bar => Assert.Equal(0, bar));
    }

    [Fact]
    public void WeekBars_trims_to_the_last_seven_days()
    {
        var bars = ScreenTimeAggregator.WeekBars(
            Enumerable.Range(1, 10).Select(day => TimeSpan.FromHours(day)).ToList());

        Assert.Equal(7, bars.Count);
        Assert.Equal(1, bars[^1], 3);
    }
}
