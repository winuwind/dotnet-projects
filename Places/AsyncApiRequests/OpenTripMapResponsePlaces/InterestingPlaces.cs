using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenTripMapResponsePlaces;

public struct InterestingPlaces
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("features")]
    public List<Feature> Features { get; set; }
}