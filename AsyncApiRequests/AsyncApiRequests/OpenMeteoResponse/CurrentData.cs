using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenMeteoResponse;

public struct CurrentData
{
    public string Time { get; set; }
    public double Precipitation { get; set; }
    
    [JsonPropertyName("temperature_2m")]
    public double Temperature2M { get; set; }

    [JsonPropertyName("relativehumidity_2m")]
    public int RelativeHumidity2M { get; set; }

    [JsonPropertyName("Precipitation_probability")]
    public int PrecipitationProbability { get; set; }

    [JsonPropertyName("windspeed_10m")]
    public double WindSpeed10M { get; set; }
    
    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; set; }
    
    [JsonPropertyName("cloudcover")]
    public int CloudCover { get; set; }
}