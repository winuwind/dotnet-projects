using System.Text.Json.Serialization;

namespace AsyncApiRequests.GraphHopperResponse;

public struct Hit
{
    public Point? Point { get; set; }
    public double[] Extent { get; set; }
    public string? Name {  get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? Postcode { get; set; }
    public string? Countrycode { get; set; }
    public string? State { get; set; }
    
    
    [JsonConverter(typeof(FlexibleStringConverter))]
    [JsonPropertyName("osm_id")]
    public string? OsmId { get; set; }
    
    [JsonPropertyName("osm_type")]
    public string? OsmType {  get; set; }
    
    [JsonPropertyName("osm_key")]
    public string? OsmKey {  get; set; }
    
    [JsonPropertyName("osm_value")]
    public string? OsmValue { get; set; }
    
}