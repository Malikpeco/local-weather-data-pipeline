using System.Globalization;
using System.Text.Json;

const string city = "Mostar";
const double latitude = 43.3438;
const double longitude = 17.8078;

string apiUrl =
    $"https://api.open-meteo.com/v1/forecast" +
    $"?latitude={latitude}" +
    $"&longitude={longitude}" +
    $"&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code";

using var httpClient = new HttpClient();

try
{
    Console.WriteLine("Extracting weather data...");


    string json = await httpClient.GetStringAsync(apiUrl);


    var apiResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    if (apiResponse?.Current is null)
    {
        Console.WriteLine("The API did not return current weather data.");
        return;
    }

    Console.WriteLine("Transforming weather data...");

    var reading = new WeatherReading
    {
        City = city,
        TemperatureCelsius = apiResponse.Current.Temperature,
        HumidityPercent = apiResponse.Current.Humidity,
        WindSpeedKmh = apiResponse.Current.WindSpeed,
        WeatherCode = apiResponse.Current.WeatherCode,
        RecordedAtUtc = DateTime.UtcNow
    };

    Console.WriteLine("Loading weather data into CSV...");

    const string filePath = "weather-readings.csv";
    bool fileExists = File.Exists(filePath);

    await using var writer = new StreamWriter(filePath, append: true);

    if (!fileExists)
    {
        await writer.WriteLineAsync(
            "City,TemperatureCelsius,HumidityPercent,WindSpeedKmh,WeatherCode,RecordedAtUtc");
    }

    string csvLine = string.Join(",",
        reading.City,
        reading.TemperatureCelsius.ToString(CultureInfo.InvariantCulture),
        reading.HumidityPercent,
        reading.WindSpeedKmh.ToString(CultureInfo.InvariantCulture),
        reading.WeatherCode,
        reading.RecordedAtUtc.ToString("O"));

    await writer.WriteLineAsync(csvLine);

    Console.WriteLine();
    Console.WriteLine("Pipeline completed successfully.");
    Console.WriteLine($"City: {reading.City}");
    Console.WriteLine($"Temperature: {reading.TemperatureCelsius} °C");
    Console.WriteLine($"Humidity: {reading.HumidityPercent}%");
    Console.WriteLine($"Wind speed: {reading.WindSpeedKmh} km/h");
    Console.WriteLine($"Saved to: {Path.GetFullPath(filePath)}");
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"API request failed: {exception.Message}");
}
catch (JsonException exception)
{
    Console.WriteLine($"Could not parse API response: {exception.Message}");
}
catch (IOException exception)
{
    Console.WriteLine($"Could not write the CSV file: {exception.Message}");
}

public class OpenMeteoResponse
{
    public CurrentWeather? Current { get; set; }
}

public class CurrentWeather
{
    [System.Text.Json.Serialization.JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
}

public class WeatherReading
{
    public string City { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public int HumidityPercent { get; set; }
    public double WindSpeedKmh { get; set; }
    public int WeatherCode { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}