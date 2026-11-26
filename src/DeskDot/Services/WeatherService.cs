using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DeskDot.Models;

namespace DeskDot.Services;

/// <summary>
/// Live weather from Open-Meteo (https://open-meteo.com). No API key, no account, and the
/// only outbound request DeskDot ever makes. Disabled entirely when
/// <see cref="AppSettings.WeatherEnabled"/> is false.
/// </summary>
public sealed class WeatherService : IDisposable
{
    private const string Endpoint = "https://api.open-meteo.com/v1/forecast";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly DispatcherTimer _timer;
    private readonly Func<bool> _isEnabled;
    private readonly Func<(double Latitude, double Longitude, bool Fahrenheit)> _query;
    private bool _refreshing;
    private bool _disposed;

    public WeatherService(Func<bool> isEnabled, Func<(double Latitude, double Longitude, bool Fahrenheit)> query)
    {
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        _query = query ?? throw new ArgumentNullException(nameof(query));

        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(20) };
        _timer.Tick += async (_, _) => await RefreshAsync().ConfigureAwait(true);
    }

    /// <summary>Raised on the UI thread with fresh data, or with <see cref="WeatherSnapshot.Empty"/> on failure.</summary>
    public event EventHandler<WeatherSnapshot>? Updated;

    /// <summary>Set when the last refresh failed, for a quiet inline message.</summary>
    public string? LastError { get; private set; }

    public void Start()
    {
        _timer.Start();
        _ = RefreshAsync();
    }

    public void Stop() => _timer.Stop();

    public async Task RefreshAsync()
    {
        if (_refreshing || !_isEnabled())
        {
            return;
        }

        var (latitude, longitude, fahrenheit) = _query();
        _refreshing = true;

        try
        {
            var url = $"{Endpoint}?latitude={latitude:0.####}&longitude={longitude:0.####}" +
                      "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,wind_direction_10m" +
                      "&daily=temperature_2m_max,temperature_2m_min&forecast_days=1" +
                      "&wind_speed_unit=kmh&timezone=auto";

            var payload = await Http.GetFromJsonAsync<OpenMeteoResponse>(url).ConfigureAwait(true);
            if (payload?.Current is null)
            {
                LastError = "Weather is unavailable right now.";
                Updated?.Invoke(this, WeatherSnapshot.Empty);
                return;
            }

            var snapshot = new WeatherSnapshot(
                Location: string.Empty, // the view model fills the friendly city name
                TemperatureC: payload.Current.Temperature,
                HighC: payload.Daily?.Max?.FirstOrDefault() ?? payload.Current.Temperature,
                LowC: payload.Daily?.Min?.FirstOrDefault() ?? payload.Current.Temperature,
                Code: payload.Current.WeatherCode,
                HumidityPercent: (int)Math.Round(payload.Current.Humidity),
                WindKph: payload.Current.WindSpeed,
                WindDirectionDegrees: payload.Current.WindDirection,
                UpdatedAt: DateTimeOffset.Now);

            LastError = null;
            Updated?.Invoke(this, snapshot);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            LastError = "Couldn't reach the weather service.";
            Updated?.Invoke(this, WeatherSnapshot.Empty);
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
    }

    /* ---------- Open-Meteo contract ---------- */

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentBlock? Current { get; set; }

        [JsonPropertyName("daily")]
        public DailyBlock? Daily { get; set; }
    }

    private sealed class CurrentBlock
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double Humidity { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_direction_10m")]
        public double WindDirection { get; set; }
    }

    private sealed class DailyBlock
    {
        [JsonPropertyName("temperature_2m_max")]
        public List<double>? Max { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public List<double>? Min { get; set; }
    }
}
