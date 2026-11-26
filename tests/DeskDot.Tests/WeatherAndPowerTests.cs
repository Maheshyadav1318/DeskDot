using DeskDot.Core;
using Xunit;

namespace DeskDot.Tests;

public class WeatherCodesTests
{
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(2, "Partly cloudy")]
    [InlineData(3, "Overcast")]
    [InlineData(45, "Fog")]
    [InlineData(61, "Rain")]
    [InlineData(71, "Snow")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(1234, "Weather")]
    public void Describe_maps_wmo_codes(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodes.Describe(code));
    }

    [Fact]
    public void Glyph_never_returns_empty()
    {
        foreach (var code in new[] { 0, 1, 2, 3, 45, 51, 61, 71, 80, 85, 95, 99, -1, 500 })
        {
            Assert.False(string.IsNullOrEmpty(WeatherCodes.Glyph(code)));
        }
    }

    [Fact]
    public void Aside_is_always_a_sentence()
    {
        foreach (var code in new[] { 0, 2, 3, 45, 51, 61, 71, 80, 95 })
        {
            var aside = WeatherCodes.Aside(code);
            Assert.False(string.IsNullOrWhiteSpace(aside));
            Assert.EndsWith(".", aside);
        }
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    [InlineData(45, "NE")]
    [InlineData(360, "N")]
    [InlineData(-90, "W")]
    [InlineData(22.5, "NNE")]
    public void CompassPoint_converts_degrees(double degrees, string expected)
    {
        Assert.Equal(expected, WeatherCodes.CompassPoint(degrees));
    }

    [Fact]
    public void CompassPoint_always_returns_a_valid_point()
    {
        var points = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };

        for (var degrees = 0; degrees < 720; degrees += 7)
        {
            Assert.Contains(WeatherCodes.CompassPoint(degrees), points);
        }
    }
}

public class PowerStatusTests
{
    [Theory]
    [InlineData(100, true, "100% Charging")]
    [InlineData(46, false, "46%")]
    [InlineData(12, true, "12% Charging")]
    [InlineData(140, false, "100%")]
    [InlineData(-5, false, "0%")]
    public void Describe_formats_battery_state(int percent, bool charging, string expected)
    {
        Assert.Equal(expected, PowerStatus.Describe(percent, charging));
    }

    [Fact]
    public void Describe_reports_desktops_without_a_battery()
    {
        Assert.Equal("No battery", PowerStatus.Describe(-1, false, hasBattery: false));
    }

    [Theory]
    [InlineData(-1, false, false, "Desktop")]
    [InlineData(80, true, true, "Charging")]
    [InlineData(80, false, true, "On battery")]
    public void StateLabel_is_short_and_clear(int percent, bool charging, bool hasBattery, string expected)
    {
        Assert.Equal(expected, PowerStatus.StateLabel(percent, charging, hasBattery));
    }

    [Theory]
    [InlineData(15, false, true, true)]
    [InlineData(15, true, true, false)]
    [InlineData(80, false, true, false)]
    [InlineData(-1, false, false, false)]
    public void IsLow_only_warns_when_unplugged_and_low(int percent, bool charging, bool hasBattery, bool expected)
    {
        Assert.Equal(expected, PowerStatus.IsLow(percent, charging, hasBattery));
    }
}
