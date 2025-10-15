namespace AsyncApiRequests.Config;

public static class AppConstants
{
    public const string GraphHopperBaseUrl = "https://graphhopper.com/api/1/geocode";
    public const string OpenMeteoBaseUrl   = "https://api.open-meteo.com/v1/forecast";
    public const string OpenTripMapBaseUrl = "https://api.opentripmap.com/0.1";

    public static readonly int PlacesRadius = int.Parse(Environment.GetEnvironmentVariable("PLACES_RADIUS") ?? "50000");
    public static readonly int PlacesLimit  = int.Parse(Environment.GetEnvironmentVariable("PLACES_LIMIT") ?? "10");
    public static readonly string TimeZone  = Environment.GetEnvironmentVariable("TIMEZONE") ?? "UTC%2B7";
    public static readonly string TempUnit =  Environment.GetEnvironmentVariable("TEMPERATURE_UNIT") ?? "celsius";
    public static readonly string WindUnit =  Environment.GetEnvironmentVariable("WINDSPEED_UNIT") ?? "ms";
    public static readonly string PrecipitationUnit =  Environment.GetEnvironmentVariable("PRECIPITATION_UNIT") ?? "mm";
    public static readonly string PressureUnit =  Environment.GetEnvironmentVariable("PRESSURE_UNIT") ?? "hpa";  
    
    public static readonly string GraphHopperApiKey = Environment.GetEnvironmentVariable("GRAPH_HOPPER_API_KEY") ?? "";
    public static readonly string OpenTripMapApiKey = Environment.GetEnvironmentVariable("OPEN_TRIPMAP_API_KEY") ?? "";
}