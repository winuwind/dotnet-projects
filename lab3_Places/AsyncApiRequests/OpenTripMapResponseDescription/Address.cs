using System.Text.Json.Serialization;

namespace AsyncApiRequests.OpenTripMapResponseDescription;

public struct Address
{
    public string City { get; set; }
    public string Road { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string CountryCode { get; set; }
    public string Neighborhood { get; set; }
    
    [JsonPropertyName("state_district")]
    public string StateDistrict { get; set; }
    
    [JsonPropertyName("house_number")]
    public string HouseNumber { get; set; }

    [JsonPropertyName("postcode")]
    public string PostalCode { get; set; }

    public void Print()
    {
        if (Country != null && Country.Length > 0)
        {
            Console.WriteLine($"Country: {Country}");
        }
        if (StateDistrict != null && StateDistrict.Length > 0)
        {
            Console.WriteLine($"State: {StateDistrict}");
        }
        if (City != null && City.Length > 0)
        {
            Console.WriteLine($"City: {City}");
        }
        if (Road != null && Road.Length > 0)
        {
            Console.WriteLine($"Road: {Road}");
        }
        if (HouseNumber != null && HouseNumber.Length > 0)
        {
            Console.WriteLine($"House number: {HouseNumber}");
        }
        if (PostalCode != null && PostalCode.Length > 0)
        {
            Console.WriteLine($"Postal code: {PostalCode}");
        }
        
    }
}