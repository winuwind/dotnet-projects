using System.Collections.Concurrent;
using System.Net.Http.Json;

using AsyncApiRequests.GraphHopperResponse;
using AsyncApiRequests.OpenMeteoResponse;
using AsyncApiRequests.OpenTripMapResponsePlaces;
using AsyncApiRequests.OpenTripMapResponseDescription;
using AsyncApiRequests.ApiRequest;

namespace AsyncApiRequests.Location;

public class LocationInfo
{
    private static string GhApiKey = "45c27055-9b34-41e7-b998-1ae3db0dee12";
    private static string OtpApiKey = "5ae2e3f221c38a28845f05b6900ef8897bdae6e43d5900fedd5f515e";
    
    private string _location;
    
    private int _id;
    private GraphHopperResponse.Point? _point;
    
    private WeatherResponse? _currentWeather;
    private WeatherResponse? _weather;
    private readonly ConcurrentDictionary<string, PlacesInfo> _interestingPlaces;

    public LocationInfo(string location)
    {
        _location = location;
        _weather = null;
        _interestingPlaces = new ConcurrentDictionary<string, PlacesInfo>();
        _id = -1;
    }

    public static void SetKeys(string ghApiKey, string otpApiKey)
    {
        GhApiKey = ghApiKey.Length > 0 ? ghApiKey : GhApiKey;
        OtpApiKey = otpApiKey.Length > 0 ? otpApiKey : OtpApiKey;
    }

    public Task GetInfo()
    {
        return GetAndPrintPlaces().ContinueWith(_ =>
        {
            if (_id == -1)
            {
                return Task.CompletedTask;
            }
            var weatherTask = GetWeather();
            var placesTask = GetInterestingPlaces();
            return Task.WhenAll(weatherTask, placesTask);
        }).Unwrap();
    }

    public void PrintInfo()
    {
        if (_id == -1)
        {
            return;
        }
        
        Console.WriteLine("Forecast: How much time do you want to view the weather for? (days * 24 + hours must be less then 5 * 24 = 120");
        Console.Write("Days {0, ..., 5}: ");
        var days = Console.ReadLine();
        int countDays;
        while (!int.TryParse(days, out countDays))
        {
            Console.Write("You must enter a number: ");
            days = Console.ReadLine();
        }
        Console.Write("Hours {0, ..., 24}: ");
        var hours = Console.ReadLine();
        int countHours;
        while (!int.TryParse(hours, out countHours))
        {
            Console.Write("You must enter a number: ");
            hours = Console.ReadLine();
        }
        countHours = int.Min(countDays * 24 + countHours, _weather?.Hourly.Time.Count ?? 0);
        
        Console.WriteLine("=======================================================================");
        Console.WriteLine($"Your location is: {_location}\nLongitude: {_point?.Lng}\nLatitude: {_point?.Lat}");
        Console.WriteLine("-----------------------------------------------------------------------");
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Weather in the selected location:");
        _currentWeather?.PrintCurrent();
        _weather?.PrintHourly(countHours);
        Console.WriteLine("-----------------------------------------------------------------------");
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Interesting places in the selected location");
        Console.ResetColor();
        foreach (var placeInfo in _interestingPlaces.Values)
        {
            placeInfo.Print();
        }
        Console.WriteLine("=======================================================================");
    }

    private Task GetAndPrintPlaces()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://graphhopper.com/api/1/geocode?q=" + _location + "&key=" + GhApiKey);
        return ApiRequestWorker.Send(request).ContinueWith(responseTask =>
            {
                var response = responseTask.Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("graphhopper: " + response.StatusCode);
                    return Task.CompletedTask;
                }

                return response.Content.ReadFromJsonAsync<PlacesData>().ContinueWith(placesTask =>
                    {
                        var placesData = placesTask.Result;
                        if (placesData == null || placesData.Hits.Length == 0)
                        {
                            Console.WriteLine("No places found");
                            return;
                        }

                        var hits = placesData.Hits;
                        for (var i = 0; i < hits.Length; i++)
                        {
                            var hit = hits[i];
                            Console.WriteLine($"Id: {i}");
                            Console.WriteLine($"Osm_id: {hit.OsmId}");
                            Console.WriteLine($"Name: {hit.Name}");
                            Console.WriteLine($"Coordinates: {hit.Point?.Lat}, {hit.Point?.Lng}");
                            Console.WriteLine($"Country: {hit.Country}");
                            if (hit.City != null)
                                Console.WriteLine($"City: {hit.City}");
                            if (hit.Street != null)
                                Console.WriteLine($"Street: {hit.Street}");
                            if (hit.HouseNumber != null)
                                Console.WriteLine($"Number of House: {hit.HouseNumber}");
                            if (hit.Postcode != null)
                                Console.WriteLine($"Postal code: {hit.Postcode}");
                            Console.WriteLine();
                        }

                        Console.Write("Enter Id of place which you want to search (-1 if you want to choose another location): ");
                        var osmId = Console.ReadLine();
                        _id = -1;
                        if (osmId != null)
                        {
                            if (!int.TryParse(osmId, out _id))
                            {
                                if (osmId == "exit")
                                    return;
                            }
                        }

                        while ((_id < 0 || _id >= hits.Length) && _id != -1)
                        {
                            Console.Write("Enter a number from {0, ..., " + (hits.Length - 1) + "}: ");
                            osmId = Console.ReadLine();
                            if (osmId != null)
                            {
                                if (!int.TryParse(osmId, out _id))
                                {
                                    if (osmId == "exit")
                                        return;
                                }
                            }
                        }

                        if (_id != -1)
                        {
                            _point = hits[_id].Point;
                            _location = hits[_id].Name ?? _location;
                        }
                    });
            })
            .Unwrap();
    }

    private Task GetHourlyWeather()
    {
        var lat = ("latitude=" + _point?.Lat).Replace(',', '.');
        var lng = ("longitude=" + _point?.Lng).Replace(',', '.');
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.open-meteo.com/v1/forecast?"
                                                             + lat + "&" + lng + 
                                                             "&hourly=temperature_2m,relativehumidity_2m,precipitation_probability,precipitation,windspeed_10m,surface_pressure,cloudcover" +
                                                             "&temperature_unit=celsius" + 
                                                             "&windspeed_unit=ms" + 
                                                             "&precipitation_unit=mm" + 
                                                             "&pressure_unit=hpa" + 
                                                             "&timezone=UTC%2B7");
        return ApiRequestWorker.Send(request).ContinueWith(responseTask =>
        {
            var response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("open-meteo: " + response.StatusCode);
                return Task.CompletedTask;
            }
            return response.Content.ReadFromJsonAsync<WeatherResponse>().ContinueWith(weatherTask =>
            {
                _weather = weatherTask.Result;
            });
        }).Unwrap();
    }
    
    private Task GetCurrentWeather()
    {
        var lat = ("latitude=" + _point?.Lat).Replace(',', '.');
        var lng = ("longitude=" + _point?.Lng).Replace(',', '.');
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.open-meteo.com/v1/forecast?"
                                                             + lat + "&" + lng + 
                                                             "&current=temperature_2m,relativehumidity_2m,precipitation_probability,precipitation,windspeed_10m,surface_pressure,cloudcover" +
                                                             "&temperature_unit=celsius" + 
                                                             "&windspeed_unit=ms" + 
                                                             "&precipitation_unit=mm" + 
                                                             "&pressure_unit=hpa" + 
                                                             "&timezone=UTC%2B7");
        return ApiRequestWorker.Send(request).ContinueWith(responseTask =>
        {
            var response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("open-meteo: " + response.StatusCode);
                return Task.CompletedTask;
            }
            return response.Content.ReadFromJsonAsync<WeatherResponse>().ContinueWith(weatherTask =>
            {
                _currentWeather = weatherTask.Result;
            });
        }).Unwrap();
    }

    private async Task GetWeather()
    {
        var taskHourly = GetHourlyWeather();
        var taskCurrent = GetCurrentWeather();
        await Task.WhenAll(taskHourly, taskCurrent);
    }

    private Task GetInterestingPlaces()
    {
        var lat = ("lat=" + _point?.Lat).Replace(',', '.');
        var lng = ("lon=" + _point?.Lng).Replace(',', '.');
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.opentripmap.com/0.1/en/places/radius?"
                                                             + "radius=50000"
                                                             + "&kinds=interesting_places"
                                                             + "&" + lng + "&" + lat
                                                             + "&limit=9"
                                                             + "&apikey=" + OtpApiKey
                                                             );
        return ApiRequestWorker.Send(request).ContinueWith(responseTask =>
        {
            var response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("opentripmap: " + response.StatusCode);
                return Task.CompletedTask;
            }
            return response.Content.ReadFromJsonAsync<InterestingPlaces>().ContinueWith(desResponseTask =>
            {
                var desResponse =  desResponseTask.Result;
                var tasks = desResponse.Features.Select(GetDescription);
                return Task.WhenAll(tasks);
            });
        }).Unwrap();
    }

    private Task GetDescription(Feature feature)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.opentripmap.com/0.1/ru/places/" +
                                                             "xid/" + feature.Properties.Xid +
                                                             "?apikey=" + OtpApiKey);
        return ApiRequestWorker.Send(request).ContinueWith(responseTask =>
        {
            var response = responseTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("opentripmap: " + response.StatusCode);
                return Task.CompletedTask;
            }
            return response.Content.ReadFromJsonAsync<PlacesInfo>().ContinueWith(placesInfoTask =>
            {
                var placesInfo = placesInfoTask.Result;
                _interestingPlaces[feature.Id] = placesInfo;
            });
        }).Unwrap();
    }
}