using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Finbridge.Backoffice.Services;

public sealed class WeatherService
{
    private readonly HttpClient _http;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient http, ILogger<WeatherService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<WeatherInfo?> GetCurrentAsync(string city = "Москва", CancellationToken ct = default)
    {
        var coords = CityCoordinates.Get(city);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={coords.Lat}&longitude={coords.Lon}" +
                  $"&current_weather=true&daily=sunrise,sunset&timezone=auto&forecast_days=1";

        try
        {
            var data = await _http.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
            if (data?.CurrentWeather is null) return null;

            DateTime? sunrise = null;
            DateTime? sunset = null;
            if (data.Daily is not null && data.Daily.Sunrise.Count > 0 && data.Daily.Sunset.Count > 0)
            {
                sunrise = DateTime.TryParse(data.Daily.Sunrise[0], out var sr) ? sr : null;
                sunset = DateTime.TryParse(data.Daily.Sunset[0], out var ss) ? ss : null;
            }

            return new WeatherInfo(
                City: city,
                TemperatureC: data.CurrentWeather.Temperature,
                WindKmh: data.CurrentWeather.Windspeed,
                WeatherCode: data.CurrentWeather.Weathercode,
                Description: WeatherCodeMap.Resolve(data.CurrentWeather.Weathercode),
                Sunrise: sunrise,
                Sunset: sunset,
                DaysUntilFullMoon: MoonPhaseCalculator.DaysUntilNextFullMoon(DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить погоду для {City}", city);
            return null;
        }
    }
}

public sealed record WeatherInfo(
    string City,
    double TemperatureC,
    double WindKmh,
    int WeatherCode,
    string Description,
    DateTime? Sunrise,
    DateTime? Sunset,
    double DaysUntilFullMoon)
{
    public string Icon => WeatherCodeMap.IconFor(WeatherCode);
    public string TemperatureDisplay => $"{TemperatureC:0}°C";
    public string? SunriseDisplay => Sunrise?.ToLocalTime().ToString("HH:mm");
    public string? SunsetDisplay => Sunset?.ToLocalTime().ToString("HH:mm");
    public string MoonDisplay => DaysUntilFullMoon switch
    {
        < 1 => "Полнолуние сегодня 🌕",
        < 1.5 => "Полнолуние tonight 🌕",
        < 2 => "Завтра полнолуние 🌕",
        _ => $"Полнолуние через {DaysUntilFullMoon:0} дн."
    };
}

internal sealed record OpenMeteoResponse(
    [property: JsonPropertyName("current_weather")] CurrentWeather? CurrentWeather,
    [property: JsonPropertyName("daily")] DailyForecast? Daily);

internal sealed record CurrentWeather(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("windspeed")] double Windspeed,
    [property: JsonPropertyName("weathercode")] int Weathercode);

internal sealed record DailyForecast(
    [property: JsonPropertyName("sunrise")] List<string> Sunrise,
    [property: JsonPropertyName("sunset")] List<string> Sunset);

internal static class WeatherCodeMap
{
    public static string Resolve(int code) => code switch
    {
        0 => "Ясно",
        1 or 2 => "Преимущ. ясно",
        3 => "Пасмурно",
        45 or 48 => "Туман",
        51 or 53 or 55 => "Морось",
        56 or 57 => "Ледяная морось",
        61 => "Слабый дождь",
        63 => "Дождь",
        65 => "Сильный дождь",
        66 or 67 => "Ледяной дождь",
        71 => "Слабый снег",
        73 => "Снег",
        75 => "Сильный снег",
        77 => "Снежные зёрна",
        80 => "Слабый ливень",
        81 => "Ливень",
        82 => "Сильный ливень",
        85 => "Снегопад",
        86 => "Сильный снегопад",
        95 => "Гроза",
        96 or 99 => "Гроза с градом",
        _ => "Неизвестно"
    };

    public static string IconFor(int code)
    {
        if (code == 0) return "☀️";
        if (code is 1 or 2) return "🌤️";
        if (code == 3) return "☁️";
        if (code is 45 or 48) return "🌫️";
        if (code is >= 51 and <= 67) return "🌧️";
        if (code is >= 71 and <= 77) return "❄️";
        if (code is >= 80 and <= 82) return "🌧️";
        if (code is 85 or 86) return "🌨️";
        if (code is >= 95) return "⛈️";
        return "🌡️";
    }
}

internal static class MoonPhaseCalculator
{
    private static readonly DateTime _referenceFullMoonUtc = new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
    private const double SynodicMonthDays = 29.530588853;

    public static double DaysUntilNextFullMoon(DateTime utcNow)
    {
        var daysSinceReference = (utcNow - _referenceFullMoonUtc).TotalDays;
        if (daysSinceReference < 0) daysSinceReference = 0;
        var cyclesCompleted = Math.Floor(daysSinceReference / SynodicMonthDays);
        var nextFullMoon = _referenceFullMoonUtc.AddDays((cyclesCompleted + 1) * SynodicMonthDays);
        var result = (nextFullMoon - utcNow).TotalDays;
        return result < 0 ? 0 : result;
    }
}

internal sealed record Coords(double Lat, double Lon);

internal static class CityCoordinates
{
    private static readonly Dictionary<string, Coords> _cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Москва"] = new(55.75, 37.62),
        ["Санкт-Петербург"] = new(59.93, 30.32),
        ["Новосибирск"] = new(55.04, 82.92),
        ["Екатеринбург"] = new(56.84, 60.61),
        ["Казань"] = new(55.79, 49.12),
        ["Челябинск"] = new(55.16, 61.40),
        ["Минск"] = new(53.90, 27.57),
        ["Припять"] = new(51.41, 30.06),
    };

    public static Coords Get(string city) =>
        _cities.TryGetValue(city, out var c) ? c : _cities["Москва"];
}
