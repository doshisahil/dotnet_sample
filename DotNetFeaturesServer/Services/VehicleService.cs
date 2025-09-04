using Microsoft.EntityFrameworkCore;
using DotNetFeaturesServer.Data;
using DotNetFeaturesServer.Models;

namespace DotNetFeaturesServer.Services;

/// <summary>
/// Service for vehicle management and business logic
/// </summary>
public interface IVehicleService
{
    Task<IEnumerable<Vehicle>> GetVehiclesAsync(int userId);
    Task<Vehicle?> GetVehicleByIdAsync(int id, int userId);
    Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, int userId);
    Task<Vehicle?> UpdateVehicleAsync(int id, Vehicle vehicle, int userId);
    Task<bool> DeleteVehicleAsync(int id, int userId);
    Task<IEnumerable<Vehicle>> SearchVehiclesAsync(string? type = null, string? brand = null, int? year = null, int userId = 0);
    Task<Dictionary<string, object>> GetVehicleStatsAsync(int userId);
}

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VehicleService> _logger;
    private readonly ICacheService _cacheService;
    private readonly ITelemetryService _telemetryService;
    
    public VehicleService(
        ApplicationDbContext context,
        ILogger<VehicleService> logger,
        ICacheService cacheService,
        ITelemetryService telemetryService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
        _telemetryService = telemetryService;
    }
    
    public async Task<IEnumerable<Vehicle>> GetVehiclesAsync(int userId)
    {
        _logger.LogInformation("Getting vehicles for user {UserId}", userId);
        
        using var activity = _telemetryService.StartActivity("GetVehicles", new Dictionary<string, object?> { ["userId"] = userId });
        
        var cacheKey = $"user_{userId}_vehicles";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
                
            _logger.LogInformation("Retrieved {Count} vehicles for user {UserId}", vehicles.Count, userId);
            await _telemetryService.RecordCustomMetricAsync("vehicles_retrieved", vehicles.Count, "count");
            
            return vehicles;
        }, TimeSpan.FromMinutes(10));
    }
    
    public async Task<Vehicle?> GetVehicleByIdAsync(int id, int userId)
    {
        _logger.LogInformation("Getting vehicle {VehicleId} for user {UserId}", id, userId);
        
        using var activity = _telemetryService.StartActivity("GetVehicleById", 
            new Dictionary<string, object?> { ["vehicleId"] = id, ["userId"] = userId });
        
        var cacheKey = $"vehicle_{id}_user_{userId}";
        
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
                
        if (vehicle == null)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found for user {UserId}", id, userId);
        }
        
        return vehicle;
    }
    
    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle, int userId)
    {
        _logger.LogInformation("Creating vehicle for user {UserId}: {Type} {Brand} {Model}", 
            userId, vehicle.Type, vehicle.Brand, vehicle.Model);
        
        using var activity = _telemetryService.StartActivity("CreateVehicle", 
            new Dictionary<string, object?> { ["userId"] = userId, ["type"] = vehicle.Type });
        
        vehicle.UserId = userId;
        vehicle.CreatedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;
        
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        await _cacheService.RemoveAsync($"user_{userId}_vehicles");
        
        _logger.LogInformation("Vehicle created with ID {VehicleId} for user {UserId}", vehicle.Id, userId);
        await _telemetryService.RecordCustomMetricAsync("vehicle_created", 1, "count");
        
        return vehicle;
    }
    
    public async Task<Vehicle?> UpdateVehicleAsync(int id, Vehicle vehicle, int userId)
    {
        _logger.LogInformation("Updating vehicle {VehicleId} for user {UserId}", id, userId);
        
        using var activity = _telemetryService.StartActivity("UpdateVehicle", 
            new Dictionary<string, object?> { ["vehicleId"] = id, ["userId"] = userId });
        
        var existingVehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
            
        if (existingVehicle == null)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found for user {UserId}", id, userId);
            return null;
        }
        
        // Update properties
        existingVehicle.Type = vehicle.Type;
        existingVehicle.Brand = vehicle.Brand;
        existingVehicle.Model = vehicle.Model;
        existingVehicle.Year = vehicle.Year;
        existingVehicle.Price = vehicle.Price;
        existingVehicle.AdditionalData = vehicle.AdditionalData;
        existingVehicle.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        await _cacheService.RemoveAsync($"user_{userId}_vehicles");
        await _cacheService.RemoveAsync($"vehicle_{id}_user_{userId}");
        
        _logger.LogInformation("Vehicle {VehicleId} updated for user {UserId}", id, userId);
        await _telemetryService.RecordCustomMetricAsync("vehicle_updated", 1, "count");
        
        return existingVehicle;
    }
    
    public async Task<bool> DeleteVehicleAsync(int id, int userId)
    {
        _logger.LogInformation("Deleting vehicle {VehicleId} for user {UserId}", id, userId);
        
        using var activity = _telemetryService.StartActivity("DeleteVehicle", 
            new Dictionary<string, object?> { ["vehicleId"] = id, ["userId"] = userId });
        
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
            
        if (vehicle == null)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found for user {UserId}", id, userId);
            return false;
        }
        
        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        await _cacheService.RemoveAsync($"user_{userId}_vehicles");
        await _cacheService.RemoveAsync($"vehicle_{id}_user_{userId}");
        
        _logger.LogInformation("Vehicle {VehicleId} deleted for user {UserId}", id, userId);
        await _telemetryService.RecordCustomMetricAsync("vehicle_deleted", 1, "count");
        
        return true;
    }
    
    public async Task<IEnumerable<Vehicle>> SearchVehiclesAsync(string? type = null, string? brand = null, int? year = null, int userId = 0)
    {
        _logger.LogInformation("Searching vehicles - Type: {Type}, Brand: {Brand}, Year: {Year}, UserId: {UserId}", 
            type, brand, year, userId);
        
        using var activity = _telemetryService.StartActivity("SearchVehicles", 
            new Dictionary<string, object?> { ["type"] = type, ["brand"] = brand, ["year"] = year, ["userId"] = userId });
        
        var query = _context.Vehicles.AsQueryable();
        
        if (userId > 0)
        {
            query = query.Where(v => v.UserId == userId);
        }
        
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(v => v.Type.ToLower().Contains(type.ToLower()));
        }
        
        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(v => v.Brand.ToLower().Contains(brand.ToLower()));
        }
        
        if (year.HasValue)
        {
            query = query.Where(v => v.Year == year.Value);
        }
        
        var vehicles = await query
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
        
        _logger.LogInformation("Found {Count} vehicles matching search criteria", vehicles.Count);
        await _telemetryService.RecordCustomMetricAsync("vehicles_searched", vehicles.Count, "count");
        
        return vehicles;
    }
    
    public async Task<Dictionary<string, object>> GetVehicleStatsAsync(int userId)
    {
        _logger.LogInformation("Getting vehicle statistics for user {UserId}", userId);
        
        using var activity = _telemetryService.StartActivity("GetVehicleStats", 
            new Dictionary<string, object?> { ["userId"] = userId });
        
        var cacheKey = $"user_{userId}_vehicle_stats";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .ToListAsync();
            
            var stats = new Dictionary<string, object>
            {
                ["TotalVehicles"] = vehicles.Count,
                ["TypeBreakdown"] = vehicles.GroupBy(v => v.Type).ToDictionary(g => g.Key, g => g.Count()),
                ["BrandBreakdown"] = vehicles.GroupBy(v => v.Brand).ToDictionary(g => g.Key, g => g.Count()),
                ["AveragePrice"] = vehicles.Any() ? vehicles.Average(v => (double)v.Price) : 0,
                ["TotalValue"] = vehicles.Sum(v => (double)v.Price),
                ["NewestVehicle"] = vehicles.OrderByDescending(v => v.Year).FirstOrDefault()?.Year,
                ["OldestVehicle"] = vehicles.OrderBy(v => v.Year).FirstOrDefault()?.Year,
                ["LastUpdated"] = DateTime.UtcNow
            };
            
            await _telemetryService.RecordCustomMetricAsync("vehicle_stats_calculated", 1, "count");
            
            return stats;
        }, TimeSpan.FromMinutes(15));
    }
}