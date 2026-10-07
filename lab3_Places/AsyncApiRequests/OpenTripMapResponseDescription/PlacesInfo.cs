namespace AsyncApiRequests.OpenTripMapResponseDescription;

public struct PlacesInfo
{
    public string Xid { get; set; }
    public string Name { get; set; }
    public string Kinds { get; set; }
    public string Osm { get; set; }
    public string Otm { get; set; }
    public string Wikipedia { get; set; }
    public string Image { get; set; }
    public string Rate { get; set; }
    public Address Address { get; set; }
    public Info Info { get; set; }
    public Point Point { get; set; }

    public void Print()
    {
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"Name: {Name}");
        Console.ResetColor();
        Console.WriteLine($"Kinds: {Kinds}");
            
        if (Wikipedia != null && Wikipedia.Length > 0)
        {
            Console.WriteLine($"Wikipedia article: {Wikipedia}");
        }

        if (Image != null && Image.Length > 0)
        {
            Console.WriteLine($"Image: {Image}");
        }
        Address.Print();
        Console.WriteLine($"Longitude: {Point.Lon}\nLatitude: {Point.Lat}");
        if(Info.Description != null && Info.Description.Length > 0)
        {
            Console.WriteLine($"Description: {Info.Description}");
        }
    }
}