using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenTripMapResponsePlaces;

public struct Properties
{
    [JsonPropertyName("xid")]
    public string Xid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("dist")]
    public double Dist { get; set; }

    [JsonPropertyName("rate")]
    public int Rate { get; set; }

    [JsonPropertyName("osm")]
    public string Osm { get; set; }

    [JsonPropertyName("kinds")]
    public string Kinds { get; set; }
}