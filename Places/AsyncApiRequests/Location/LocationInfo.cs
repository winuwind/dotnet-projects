using System.Collections.Concurrent;
using System.Net.Http.Json;

using AsyncApiRequests.GraphHopperResponse;
using AsyncApiRequests.OpenMeteoResponse;
using AsyncApiRequests.OpenTripMapResponsePlaces;
using AsyncApiRequests.OpenTripMapResponseDescription;
using AsyncApiRequests.ApiRequest;
using AsyncApiRequests.Config;

namespace AsyncApiRequests.Location;

public class LocationInfo
{
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
        
        Console.WriteLine("Forecast: How much time do you want to view the weather for? (days * 24 + hours must be less then 5 * 24 = 120)");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.GraphHopperBaseUrl}?q={_location}&limit={AppConstants.LimitLocation}&key={AppConstants.GraphHopperApiKey}");
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
                            Console.WriteLine($"Location number: {i}");
                            Console.WriteLine($"Type of location: {hit.OsmKey}, {hit.OsmValue}");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.OpenMeteoBaseUrl}?{lat}&{lng}" +
                                                             "&hourly=temperature_2m,relativehumidity_2m,precipitation_probability,precipitation,windspeed_10m,surface_pressure,cloudcover" +
                                                             $"&temperature_unit={AppConstants.TempUnit}" + 
                                                             $"&windspeed_unit={AppConstants.WindUnit}" + 
                                                             $"&precipitation_unit={AppConstants.PrecipitationUnit}" + 
                                                             $"&pressure_unit={AppConstants.PressureUnit}" + 
                                                             $"&timezone={AppConstants.TimeZone}");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.OpenMeteoBaseUrl}?{lat}&{lng}" +
                                                             "&current=temperature_2m,relativehumidity_2m,precipitation_probability,precipitation,windspeed_10m,surface_pressure,cloudcover" +
                                                             $"&temperature_unit={AppConstants.TempUnit}" + 
                                                             $"&windspeed_unit={AppConstants.WindUnit}" + 
                                                             $"&precipitation_unit={AppConstants.PrecipitationUnit}" + 
                                                             $"&pressure_unit={AppConstants.PressureUnit}" + 
                                                             $"&timezone={AppConstants.TimeZone}");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.OpenTripMapBaseUrl}/ru/places/radius?" +
                                                             $"radius={AppConstants.PlacesRadius}" +
                                                             "&kinds=interesting_places" +
                                                             $"&{lat}&{lng}" +
                                                             $"&limit={AppConstants.PlacesLimit}" +
                                                             $"&apikey={AppConstants.OpenTripMapApiKey}");
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
                if (desResponse.Features == null)
                {
                    Console.WriteLine("Zero interesting places were founded or error on opentripmap");
                    return Task.CompletedTask;
                }
                var tasks = desResponse.Features.Select(GetDescription);
                return Task.WhenAll(tasks);
            });
        }).Unwrap();
    }

    private Task GetDescription(Feature feature)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.OpenTripMapBaseUrl}/ru/places/" +
                                                             $"xid/{feature.Properties.Xid}" +
                                                             $"?apikey={AppConstants.OpenTripMapApiKey}");
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