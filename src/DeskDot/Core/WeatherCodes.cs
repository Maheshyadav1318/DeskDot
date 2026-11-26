namespace DeskDot.Core;

/// <summary>
/// Maps Open-Meteo / WMO weather interpretation codes to the card's copy and glyph.
/// Pure so the mapping is unit tested rather than buried in a view model.
/// </summary>
public static class WeatherCodes
{
    /// <summary>Short description, e.g. "Partly cloudy".</summary>
    public static string Describe(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snow",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Weather"
    };

    /// <summary>A Segoe MDL2 Assets glyph for the condition.</summary>
    public static string Glyph(int code) => code switch
    {
        0 or 1 => "\uE706",                       // Sunny
        2 => "\uE707",                            // Partly cloudy
        3 => "\uE753",                            // Cloudy
        45 or 48 => "\uE9C0",                     // Fog
        >= 51 and <= 57 => "\uE9C4",              // Drizzle
        >= 61 and <= 67 => "\uE9C4",              // Rain
        >= 71 and <= 77 => "\uE9C6",              // Snow
        >= 80 and <= 82 => "\uE9C4",              // Showers
        >= 85 and <= 86 => "\uE9C6",              // Snow showers
        >= 95 => "\uE9C8",                        // Storm
        _ => "\uE753"
    };

    /// <summary>The card's second line: description plus a gentle aside.</summary>
    public static string Aside(int code) => code switch
    {
        0 => "Not a cloud. A little possibility.",
        1 or 2 => "A little possibility.",
        3 => "Cozy weather for getting things done.",
        45 or 48 => "Everything's a bit softer today.",
        51 or 53 or 55 => "A small drizzle. Take a jacket.",
        56 or 57 => "Slippery out. Mind your step.",
        61 or 63 or 65 => "Rain. A good day to stay in.",
        66 or 67 => "Icy rain. Keep warm.",
        71 or 73 or 75 or 77 => "Snow. Take it slow.",
        80 or 81 or 82 => "Passing showers. Breathe it in.",
        85 or 86 => "Snow showers. Wrap up.",
        95 or 96 or 99 => "Storms about. Stay safe.",
        _ => "A little weather, a little room."
    };

    /// <summary>
    /// Compass direction from degrees, for the wind read-out (16 points).
    /// </summary>
    public static string CompassPoint(double degrees)
    {
        var normalised = ((degrees % 360) + 360) % 360;
        var index = (int)Math.Round(normalised / 22.5, MidpointRounding.AwayFromZero) % 16;
        return new[]
        {
            "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
        }[index];
    }
}
