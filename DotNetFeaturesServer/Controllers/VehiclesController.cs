using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DotNetFeaturesServer.Models;
using DotNetFeaturesServer.Services;
using System.ComponentModel.DataAnnotations;

namespace DotNetFeaturesServer.Controllers;

/// <summary>
/// Controller for vehicle management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<VehiclesController> _logger;
    
    public VehiclesController(
        IVehicleService vehicleService,
        ITelemetryService telemetryService,
        ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _telemetryService = telemetryService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all vehicles for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVehicles()
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            var vehicles = await _vehicleService.GetVehiclesAsync(userId);
            await LogApiCall(userId, "vehicles", "GET", 200, startTime);
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicles for user {UserId}", userId);
            await LogApiCall(userId, "vehicles", "GET", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Get a specific vehicle by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicle(int id)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id, userId);
            if (vehicle == null)
            {
                await LogApiCall(userId, $"vehicles/{id}", "GET", 404, startTime);
                return NotFound(new { message = "Vehicle not found" });
            }
            
            await LogApiCall(userId, $"vehicles/{id}", "GET", 200, startTime);
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle {VehicleId} for user {UserId}", id, userId);
            await LogApiCall(userId, $"vehicles/{id}", "GET", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Create a new vehicle
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            if (!ModelState.IsValid)
            {
                await LogApiCall(userId, "vehicles", "POST", 400, startTime);
                return BadRequest(ModelState);
            }
            
            var vehicle = new Vehicle
            {
                Type = request.Type,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
                Price = request.Price,
                AdditionalData = request.AdditionalData ?? "{}"
            };
            
            var createdVehicle = await _vehicleService.CreateVehicleAsync(vehicle, userId);
            
            await LogApiCall(userId, "vehicles", "POST", 201, startTime);
            return CreatedAtAction(nameof(GetVehicle), new { id = createdVehicle.Id }, createdVehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle for user {UserId}", userId);
            await LogApiCall(userId, "vehicles", "POST", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Update an existing vehicle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleRequest request)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            if (!ModelState.IsValid)
            {
                await LogApiCall(userId, $"vehicles/{id}", "PUT", 400, startTime);
                return BadRequest(ModelState);
            }
            
            var vehicle = new Vehicle
            {
                Type = request.Type,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
                Price = request.Price,
                AdditionalData = request.AdditionalData ?? "{}"
            };
            
            var updatedVehicle = await _vehicleService.UpdateVehicleAsync(id, vehicle, userId);
            if (updatedVehicle == null)
            {
                await LogApiCall(userId, $"vehicles/{id}", "PUT", 404, startTime);
                return NotFound(new { message = "Vehicle not found" });
            }
            
            await LogApiCall(userId, $"vehicles/{id}", "PUT", 200, startTime);
            return Ok(updatedVehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle {VehicleId} for user {UserId}", id, userId);
            await LogApiCall(userId, $"vehicles/{id}", "PUT", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Delete a vehicle
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            var success = await _vehicleService.DeleteVehicleAsync(id, userId);
            if (!success)
            {
                await LogApiCall(userId, $"vehicles/{id}", "DELETE", 404, startTime);
                return NotFound(new { message = "Vehicle not found" });
            }
            
            await LogApiCall(userId, $"vehicles/{id}", "DELETE", 204, startTime);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle {VehicleId} for user {UserId}", id, userId);
            await LogApiCall(userId, $"vehicles/{id}", "DELETE", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Search vehicles with filters
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchVehicles([FromQuery] string? type = null, [FromQuery] string? brand = null, [FromQuery] int? year = null)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            var vehicles = await _vehicleService.SearchVehiclesAsync(type, brand, year, userId);
            await LogApiCall(userId, "vehicles/search", "GET", 200, startTime);
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vehicles for user {UserId}", userId);
            await LogApiCall(userId, "vehicles/search", "GET", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Get vehicle statistics for the authenticated user
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetVehicleStats()
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        
        try
        {
            var stats = await _vehicleService.GetVehicleStatsAsync(userId);
            await LogApiCall(userId, "vehicles/stats", "GET", 200, startTime);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle stats for user {UserId}", userId);
            await LogApiCall(userId, "vehicles/stats", "GET", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
    
    private async Task LogApiCall(int userId, string endpoint, string method, int statusCode, DateTime startTime)
    {
        var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        
        await _telemetryService.LogApiCallAsync(userId, $"api/{endpoint}", method, statusCode, duration, 
            "", "", userAgent, ipAddress);
    }
}

// Request models
public class CreateVehicleRequest
{
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;
    
    [Range(1900, 2030)]
    public int Year { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    
    public string? AdditionalData { get; set; }
}

public class UpdateVehicleRequest
{
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;
    
    [Range(1900, 2030)]
    public int Year { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    
    public string? AdditionalData { get; set; }
}