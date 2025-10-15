using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenMeteoResponse;

public struct WeatherResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; }
    public double Elevation { get; set; }
    public HourlyData Hourly { get; set; }
    public CurrentData Current { get; set; }

    [JsonPropertyName("Generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; }

    [JsonPropertyName("hourly_units")]
    public Units HourlyUnits { get; set; }
    
    [JsonPropertyName("current_units")]
    public Units CurrentUnits { get; set; }

    public void PrintCurrent()
    {
        Console.BackgroundColor = ConsoleColor.Green;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Now:");
        Console.WriteLine($"Temperature: {Current.Temperature2M} {CurrentUnits.Temperature2M}");
        Console.WriteLine($"Wind: {Current.WindSpeed10M} {CurrentUnits.WindSpeed10M}");
        Console.WriteLine($"Cloud cover: {Current.CloudCover} {CurrentUnits.CloudCover}");
        Console.WriteLine($"Precipitation: {Current.Precipitation} {CurrentUnits.Precipitation}");
        Console.WriteLine($"Surface pressure: {Current.SurfacePressure} {CurrentUnits.SurfacePressure}");
        Console.WriteLine($"Relative humidity: {Current.RelativeHumidity2M} {CurrentUnits.RelativeHumidity2M}");
        Console.ResetColor();
    }

    public void PrintHourly(int countHours)
    {
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Forecast:");
        Console.ResetColor();
        for (var i = 0; i < countHours; i++)
        {
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Time: {Hourly.Time[i]}");
            Console.ResetColor();
            Console.WriteLine($"Temperature: {Hourly.Temperature2M[i]} {HourlyUnits.Temperature2M}");
            Console.WriteLine($"Wind: {Hourly.WindSpeed10M[i]} {HourlyUnits.WindSpeed10M}");
            Console.WriteLine($"Cloud cover: {Hourly.CloudCover[i]} {HourlyUnits.CloudCover}");
            Console.WriteLine($"Precipitation: {Hourly.Precipitation[i]} {HourlyUnits.Precipitation}");
            Console.WriteLine($"Surface pressure: {Hourly.SurfacePressure[i]} {HourlyUnits.SurfacePressure}");
            Console.WriteLine($"Relative humidity: {Hourly.RelativeHumidity2M[i]} {HourlyUnits.RelativeHumidity2M}");
        }
    }
}