using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using DotNetFeaturesServer.Data;
using DotNetFeaturesServer.Models;

namespace DotNetFeaturesServer.Services;

/// <summary>
/// Service for custom telemetry and metrics collection
/// </summary>
public interface ITelemetryService
{
    Task LogApiCallAsync(int userId, string endpoint, string method, int statusCode, long durationMs, 
        string requestBody = "", string responseBody = "", string userAgent = "", string ipAddress = "");
    Task RecordCustomMetricAsync(string metricName, double value, string unit = "", Dictionary<string, string>? tags = null);
    void IncrementCounter(string name, Dictionary<string, object?>? tags = null);
    void RecordHistogram(string name, double value, Dictionary<string, object?>? tags = null);
    void SetGauge(string name, double value, Dictionary<string, object?>? tags = null);
    Activity? StartActivity(string name, Dictionary<string, object?>? tags = null);
    Task<Dictionary<string, object>> GetMetricsStatsAsync();
}

public class TelemetryService : ITelemetryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TelemetryService> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _apiCallCounter;
    private readonly Histogram<double> _apiDurationHistogram;
    private readonly Gauge<double> _activeConnectionsGauge;
    private readonly Counter<long> _errorCounter;
    
    // ActivitySource for distributed tracing
    private static readonly ActivitySource ActivitySource = new("DotNetFeaturesServer");
    
    public TelemetryService(ApplicationDbContext context, ILogger<TelemetryService> logger)
    {
        _context = context;
        _logger = logger;
        _meter = new Meter("DotNetFeaturesServer");
        
        // Initialize custom metrics
        _apiCallCounter = _meter.CreateCounter<long>(
            "api_calls_total",
            description: "Total number of API calls made");
            
        _apiDurationHistogram = _meter.CreateHistogram<double>(
            "api_duration_ms",
            description: "Duration of API calls in milliseconds");
            
        _activeConnectionsGauge = _meter.CreateGauge<double>(
            "active_connections",
            description: "Number of active connections");
            
        _errorCounter = _meter.CreateCounter<long>(
            "api_errors_total",
            description: "Total number of API errors");
    }
    
    public async Task LogApiCallAsync(int userId, string endpoint, string method, int statusCode, long durationMs,
        string requestBody = "", string responseBody = "", string userAgent = "", string ipAddress = "")
    {
        try
        {
            _logger.LogInformation("Logging API call - User: {UserId}, Endpoint: {Endpoint}, Method: {Method}, Status: {StatusCode}, Duration: {Duration}ms",
                userId, endpoint, method, statusCode, durationMs);
            
            // Store in database
            var apiLog = new ApiLog
            {
                UserId = userId,
                Endpoint = endpoint,
                Method = method,
                StatusCode = statusCode,
                DurationMs = durationMs,
                RequestTime = DateTime.UtcNow,
                RequestBody = requestBody.Length > 1000 ? requestBody[..1000] + "..." : requestBody,
                ResponseBody = responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
                UserAgent = userAgent,
                IpAddress = ipAddress
            };
            
            _context.ApiLogs.Add(apiLog);
            await _context.SaveChangesAsync();
            
            // Record metrics
            var tags = new Dictionary<string, object?>
            {
                ["endpoint"] = endpoint,
                ["method"] = method,
                ["status_code"] = statusCode.ToString(),
                ["user_id"] = userId.ToString()
            };
            
            _apiCallCounter.Add(1, [.. tags]);
            _apiDurationHistogram.Record(durationMs, [.. tags]);
            
            if (statusCode >= 400)
            {
                _errorCounter.Add(1, [.. tags]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging API call");
        }
    }
    
    public async Task RecordCustomMetricAsync(string metricName, double value, string unit = "", Dictionary<string, string>? tags = null)
    {
        try
        {
            _logger.LogDebug("Recording custom metric: {MetricName} = {Value} {Unit}", metricName, value, unit);
            
            var performanceMetric = new PerformanceMetric
            {
                MetricName = metricName,
                Value = value,
                Unit = unit,
                Timestamp = DateTime.UtcNow,
                Tags = tags != null ? System.Text.Json.JsonSerializer.Serialize(tags) : "{}"
            };
            
            _context.PerformanceMetrics.Add(performanceMetric);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording custom metric: {MetricName}", metricName);
        }
    }
    
    public void IncrementCounter(string name, Dictionary<string, object?>? tags = null)
    {
        try
        {
            var counter = _meter.CreateCounter<long>(name);
            counter.Add(1, [.. (tags ?? new Dictionary<string, object?>())]);
            
            _logger.LogDebug("Incremented counter: {CounterName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter: {CounterName}", name);
        }
    }
    
    public void RecordHistogram(string name, double value, Dictionary<string, object?>? tags = null)
    {
        try
        {
            var histogram = _meter.CreateHistogram<double>(name);
            histogram.Record(value, [.. (tags ?? new Dictionary<string, object?>())]);
            
            _logger.LogDebug("Recorded histogram: {HistogramName} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram: {HistogramName}", name);
        }
    }
    
    public void SetGauge(string name, double value, Dictionary<string, object?>? tags = null)
    {
        try
        {
            var gauge = _meter.CreateGauge<double>(name);
            // Note: Gauges are typically set through callbacks, this is a simplified implementation
            
            _logger.LogDebug("Set gauge: {GaugeName} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting gauge: {GaugeName}", name);
        }
    }
    
    public Activity? StartActivity(string name, Dictionary<string, object?>? tags = null)
    {
        try
        {
            var activity = ActivitySource.StartActivity(name);
            
            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value?.ToString());
                }
            }
            
            _logger.LogDebug("Started activity: {ActivityName}", name);
            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting activity: {ActivityName}", name);
            return null;
        }
    }
    
    public async Task<Dictionary<string, object>> GetMetricsStatsAsync()
    {
        try
        {
            var stats = new Dictionary<string, object>();
            
            // Get API call statistics from database
            var totalApiCalls = await _context.ApiLogs.CountAsync();
            var errorCount = await _context.ApiLogs.CountAsync(l => l.StatusCode >= 400);
            var avgDuration = await _context.ApiLogs.AverageAsync(l => (double?)l.DurationMs) ?? 0;
            
            // Get recent performance metrics
            var recentMetrics = await _context.PerformanceMetrics
                .Where(m => m.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .GroupBy(m => m.MetricName)
                .Select(g => new { MetricName = g.Key, Count = g.Count(), LatestValue = g.OrderByDescending(m => m.Timestamp).First().Value })
                .ToListAsync();
            
            stats["TotalApiCalls"] = totalApiCalls;
            stats["ErrorCount"] = errorCount;
            stats["ErrorRate"] = totalApiCalls > 0 ? (double)errorCount / totalApiCalls : 0;
            stats["AverageDurationMs"] = avgDuration;
            stats["RecentMetrics"] = recentMetrics;
            stats["LastUpdated"] = DateTime.UtcNow;
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics stats");
            return new Dictionary<string, object> { ["Error"] = ex.Message };
        }
    }
}