using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenTripMapResponseDescription;

public struct Info
{
    [JsonPropertyName("descr")]
    public string Description { get; set; }
}