using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenMeteoResponse;

public struct HourlyData
{
    public List<string> Time { get; set; }
    public List<double> Precipitation { get; set; }
    
    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature2M { get; set; }

    [JsonPropertyName("relativehumidity_2m")]
    public List<int> RelativeHumidity2M { get; set; }

    [JsonPropertyName("Precipitation_probability")]
    public List<int> PrecipitationProbability { get; set; }

    [JsonPropertyName("windspeed_10m")]
    public List<double> WindSpeed10M { get; set; }
    
    [JsonPropertyName("surface_pressure")]
    public List<double> SurfacePressure { get; set; }
    
    [JsonPropertyName("cloudcover")]
    public List<int> CloudCover { get; set; }
}