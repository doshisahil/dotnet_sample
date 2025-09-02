using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using DotNetFeaturesSample.Models;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service demonstrating JSON serialization with polymorphism support
/// </summary>
public interface IJsonSerializationService
{
    Task<string> SerializeVehiclesAsync(List<Vehicle> vehicles);
    Task<List<Vehicle>?> DeserializeVehiclesAsync(string json);
    Task DemonstratePolymorphismAsync();
}

public class JsonSerializationService : IJsonSerializationService
{
    private readonly ILogger<JsonSerializationService> _logger;
    private readonly JsonSerializerOptions _options;

    public JsonSerializationService(ILogger<JsonSerializationService> logger)
    {
        _logger = logger;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = false
        };
    }

    public async Task<string> SerializeVehiclesAsync(List<Vehicle> vehicles)
    {
        _logger.LogInformation("Serializing {Count} vehicles with polymorphism support", vehicles.Count);
        
        try
        {
            // Add vehicle type to each object for deserialization
            var vehicleData = vehicles.Select(v => new
            {
                Type = v.VehicleType,
                Id = v.Id,
                Brand = v.Brand,
                Data = v switch
                {
                    Car car => (object)new { car.NumberOfDoors },
                    Motorcycle motorcycle => new { motorcycle.HasSidecar },
                    _ => new { }
                }
            }).ToList();

            var json = JsonSerializer.Serialize(vehicleData, _options);
            _logger.LogInformation("Serialization completed successfully");
            return await Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize vehicles");
            throw;
        }
    }

    public async Task<List<Vehicle>?> DeserializeVehiclesAsync(string json)
    {
        _logger.LogInformation("Deserializing vehicles from JSON");
        
        try
        {
            using var document = JsonDocument.Parse(json);
            var vehicles = new List<Vehicle>();

            foreach (var element in document.RootElement.EnumerateArray())
            {
                var type = element.GetProperty("Type").GetString();
                var id = element.GetProperty("Id").GetString() ?? "";
                var brand = element.GetProperty("Brand").GetString() ?? "";
                var data = element.GetProperty("Data");

                Vehicle vehicle = type switch
                {
                    "Car" => new Car
                    {
                        Id = id,
                        Brand = brand,
                        NumberOfDoors = data.GetProperty("NumberOfDoors").GetInt32()
                    },
                    "Motorcycle" => new Motorcycle
                    {
                        Id = id,
                        Brand = brand,
                        HasSidecar = data.GetProperty("HasSidecar").GetBoolean()
                    },
                    _ => throw new NotSupportedException($"Vehicle type {type} not supported")
                };

                vehicles.Add(vehicle);
            }

            _logger.LogInformation("Deserialization completed successfully, found {Count} vehicles", vehicles.Count);
            return await Task.FromResult(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize vehicles");
            return null;
        }
    }

    public async Task DemonstratePolymorphismAsync()
    {
        _logger.LogInformation("Demonstrating polymorphic JSON serialization");

        var vehicles = new List<Vehicle>
        {
            new Car { Id = "1", Brand = "Toyota", NumberOfDoors = 4 },
            new Motorcycle { Id = "2", Brand = "Harley Davidson", HasSidecar = false },
            new Car { Id = "3", Brand = "BMW", NumberOfDoors = 2 }
        };

        // Serialize
        var json = await SerializeVehiclesAsync(vehicles);
        _logger.LogInformation("Serialized JSON:\\n{Json}", json);

        // Deserialize
        var deserializedVehicles = await DeserializeVehiclesAsync(json);
        
        if (deserializedVehicles != null)
        {
            foreach (var vehicle in deserializedVehicles)
            {
                _logger.LogInformation("Deserialized vehicle: {Type} - {Brand} (ID: {Id})", 
                    vehicle.VehicleType, vehicle.Brand, vehicle.Id);
                
                // Demonstrate polymorphic behavior
                switch (vehicle)
                {
                    case Car car:
                        _logger.LogInformation("Car has {Doors} doors", car.NumberOfDoors);
                        break;
                    case Motorcycle motorcycle:
                        _logger.LogInformation("Motorcycle has sidecar: {HasSidecar}", motorcycle.HasSidecar);
                        break;
                }
            }
        }
    }
}