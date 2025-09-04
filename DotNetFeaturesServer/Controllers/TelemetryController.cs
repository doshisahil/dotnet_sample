using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotNetFeaturesServer.Services;
using System.Diagnostics;

namespace DotNetFeaturesServer.Controllers;

/// <summary>
/// Controller for telemetry and monitoring endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TelemetryController> _logger;
    
    public TelemetryController(
        ITelemetryService telemetryService,
        ICacheService cacheService,
        ILogger<TelemetryController> logger)
    {
        _telemetryService = telemetryService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get application health status
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var health = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                WorkingSet = GC.GetTotalMemory(false),
                Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
            };
            
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get detailed application metrics
    /// </summary>
    [HttpGet("metrics")]
    [Authorize]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var metrics = await _telemetryService.GetMetricsStatsAsync();
            var cacheStats = await _cacheService.GetCacheStatsAsync();
            
            var response = new
            {
                Metrics = metrics,
                Cache = cacheStats,
                System = new
                {
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    GCMemory = GC.GetTotalMemory(false),
                    ThreadCount = Process.GetCurrentProcess().Threads.Count,
                    HandleCount = Process.GetCurrentProcess().HandleCount
                },
                Timestamp = DateTime.UtcNow
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Record a custom metric
    /// </summary>
    [HttpPost("metrics")]
    [Authorize]
    public async Task<IActionResult> RecordMetric([FromBody] RecordMetricRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            await _telemetryService.RecordCustomMetricAsync(request.MetricName, request.Value, request.Unit, request.Tags);
            
            return Ok(new { message = "Metric recorded successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording custom metric {MetricName}", request.MetricName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get cache statistics and management
    /// </summary>
    [HttpGet("cache")]
    [Authorize]
    public async Task<IActionResult> GetCacheStats()
    {
        try
        {
            var stats = await _cacheService.GetCacheStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache stats");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Clear the cache
    /// </summary>
    [HttpDelete("cache")]
    [Authorize]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            await _cacheService.ClearAsync();
            _logger.LogInformation("Cache cleared by user request");
            return Ok(new { message = "Cache cleared successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get Windows Event Log demonstration
    /// </summary>
    [HttpPost("windows-event")]
    [Authorize]
    public async Task<IActionResult> LogWindowsEvent([FromBody] WindowsEventRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // Demonstrate logging to Windows Event Log (only works on Windows)
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var eventLog = new System.Diagnostics.EventLog("Application");
                    eventLog.Source = "DotNetFeaturesServer";
                    
                    var eventType = request.Level.ToLower() switch
                    {
                        "error" => System.Diagnostics.EventLogEntryType.Error,
                        "warning" => System.Diagnostics.EventLogEntryType.Warning,
                        _ => System.Diagnostics.EventLogEntryType.Information
                    };
                    
                    eventLog.WriteEntry(request.Message, eventType, request.EventId);
                    
                    _logger.LogInformation("Successfully logged to Windows Event Log: {Message}", request.Message);
                    
                    return Ok(new 
                    { 
                        message = "Event logged to Windows Event Log successfully",
                        eventId = request.EventId,
                        level = request.Level,
                        timestamp = DateTime.UtcNow 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to Windows Event Log");
                    return BadRequest(new { error = "Failed to write to Windows Event Log", details = ex.Message });
                }
            }
            else
            {
                _logger.LogInformation("Windows Event Log demonstration requested on non-Windows platform: {Message}", request.Message);
                return Ok(new 
                { 
                    message = "Windows Event Log is only available on Windows platform. Logged to application logger instead.",
                    platform = Environment.OSVersion.Platform.ToString(),
                    timestamp = DateTime.UtcNow 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Windows Event Log demonstration");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Demonstrate various .NET 8 logging features
    /// </summary>
    [HttpPost("logging-demo")]
    [Authorize]
    public async Task<IActionResult> LoggingDemo([FromBody] LoggingDemoRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // Demonstrate structured logging with various features
            using var scope = _logger.BeginScope("LoggingDemo-{CorrelationId}", Guid.NewGuid());
            
            // Log with different levels
            _logger.LogTrace("This is a trace message: {Message}", request.Message);
            _logger.LogDebug("This is a debug message: {Message}", request.Message);
            _logger.LogInformation("This is an information message: {Message}", request.Message);
            _logger.LogWarning("This is a warning message: {Message}", request.Message);
            
            // Structured logging with complex objects
            _logger.LogInformation("Logging complex object: {@Request}", request);
            
            // High-performance logging with LoggerMessage
            LogHighPerformanceMessage(_logger, request.Message, DateTime.UtcNow, null);
            
            // Custom telemetry
            await _telemetryService.RecordCustomMetricAsync("logging_demo_executed", 1, "count");
            _telemetryService.IncrementCounter("logging_demo_calls");
            
            var response = new
            {
                message = "Logging demonstration completed successfully",
                features = new[]
                {
                    "Structured logging with templates",
                    "Scoped logging context",
                    "Multiple log levels (Trace, Debug, Info, Warning)",
                    "Complex object serialization",
                    "High-performance logging with LoggerMessage",
                    "Custom telemetry integration"
                },
                timestamp = DateTime.UtcNow,
                correlationId = System.Diagnostics.Activity.Current?.Id
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in logging demonstration");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    // High-performance logging using LoggerMessage
    private static readonly Action<ILogger, string, DateTime, Exception?> LogHighPerformanceMessage =
        LoggerMessage.Define<string, DateTime>(
            LogLevel.Information,
            new EventId(1001, "HighPerformanceLog"),
            "High-performance log entry: {Message} at {Timestamp}");
}

// Request models
public class RecordMetricRequest
{
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public Dictionary<string, string>? Tags { get; set; }
}

public class WindowsEventRequest
{
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Information";
    public int EventId { get; set; } = 1000;
}

public class LoggingDemoRequest
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? AdditionalData { get; set; }
}