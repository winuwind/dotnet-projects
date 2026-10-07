using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenMeteoResponse;

public struct Units
{
    public string Time { get; set; }
    public string Precipitation { get; set; }
    
    [JsonPropertyName("temperature_2m")]
    public string Temperature2M { get; set; }

    [JsonPropertyName("relativehumidity_2m")]
    public string RelativeHumidity2M { get; set; }

    [JsonPropertyName("precipitation_probability")]
    public string PrecipitationProbability { get; set; }

    [JsonPropertyName("windspeed_10m")]
    public string WindSpeed10M { get; set; }
    
    [JsonPropertyName("surface_pressure")]
    public string SurfacePressure { get; set; }
    
    [JsonPropertyName("cloudcover")]
    public string CloudCover { get; set; }
}