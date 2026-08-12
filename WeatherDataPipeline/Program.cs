using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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




var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
string? connectionString = configuration.GetConnectionString("DefaultConnection");

if(string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Database connection string not found.");
    return;
}    





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

    
    Console.WriteLine("Loading weather data into SQL Server...");
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    Console.WriteLine("Connected to SQL Server successfully.");

    string insertSql = """
    INSERT INTO dbo.WeatherReadings
    (
        City,
        TemperatureCelsius,
        HumidityPercent,
        WindSpeedKmh,
        WeatherCode,
        RecordedAtUtc
    )
    VALUES
    (
        @City,
        @TemperatureCelsius,
        @HumidityPercent,
        @WindSpeedKmh,
        @WeatherCode,
        @RecordedAtUtc
    );
    """;

    await using var command = new SqlCommand(insertSql, connection);

    command.Parameters.AddWithValue("@City", reading.City);
    command.Parameters.AddWithValue("@TemperatureCelsius", reading.TemperatureCelsius);
    command.Parameters.AddWithValue("@HumidityPercent", reading.HumidityPercent);
    command.Parameters.AddWithValue("@WindSpeedKmh", reading.WindSpeedKmh);
    command.Parameters.AddWithValue("@WeatherCode", reading.WeatherCode);
    command.Parameters.AddWithValue("@RecordedAtUtc", reading.RecordedAtUtc);

    await command.ExecuteNonQueryAsync();



    Console.WriteLine();
    Console.WriteLine("Pipeline completed successfully.");
    Console.WriteLine($"City: {reading.City}");
    Console.WriteLine($"UTC Time: {reading.RecordedAtUtc}");
    Console.WriteLine($"Temperature: {reading.TemperatureCelsius} °C");
    Console.WriteLine($"Humidity: {reading.HumidityPercent}%");
    Console.WriteLine($"Wind speed: {reading.WindSpeedKmh} km/h");
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"API request failed: {exception.Message}");
}
catch (JsonException exception)
{
    Console.WriteLine($"Could not parse API response: {exception.Message}");
}
catch (SqlException exception)
{
    Console.WriteLine($"Database operation failed: {exception.Message}");
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