using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenTripMapResponsePlaces;

public struct Geometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; }
}