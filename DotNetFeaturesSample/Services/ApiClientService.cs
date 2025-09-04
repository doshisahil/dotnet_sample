using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service for connecting to the DotNet Features Server API
/// </summary>
public interface IApiClientService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<IEnumerable<dynamic>?> GetVehiclesAsync();
    Task<dynamic?> CreateVehicleAsync(dynamic vehicle);
    Task<dynamic?> GetTelemetryAsync();
    Task<bool> RecordMetricAsync(string metricName, double value, string unit = "");
    Task<dynamic?> GetHealthAsync();
    Task<bool> TestWindowsEventLogAsync(string message);
}

public class ApiClientService : IApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClientService> _logger;
    private readonly IConfiguration _configuration;
    private string? _jwtToken;
    
    public ApiClientService(HttpClient httpClient, ILogger<ApiClientService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        var baseUrl = _configuration["ServerApi:BaseUrl"] ?? "https://localhost:7000";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DotNetFeaturesSample/1.0");
    }
    
    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Authenticating with server as user: {Username}", username);
            
            var loginRequest = new
            {
                Username = username,
                Password = password
            };
            
            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (loginResponse.TryGetProperty("token", out var tokenElement))
                {
                    _jwtToken = tokenElement.GetString();
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
                    
                    _logger.LogInformation("Successfully authenticated with server");
                    return true;
                }
            }
            
            _logger.LogWarning("Authentication failed. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return false;
        }
    }
    
    public async Task<IEnumerable<dynamic>?> GetVehiclesAsync()
    {
        try
        {
            _logger.LogInformation("Getting vehicles from server");
            
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogWarning("Not authenticated. Cannot get vehicles.");
                return null;
            }
            
            var response = await _httpClient.GetAsync("/api/vehicles");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var vehicles = JsonSerializer.Deserialize<JsonElement[]>(content);
                
                _logger.LogInformation("Retrieved {Count} vehicles from server", vehicles?.Length ?? 0);
                return vehicles?.Cast<dynamic>();
            }
            
            _logger.LogWarning("Failed to get vehicles. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicles from server");
            return null;
        }
    }
    
    public async Task<dynamic?> CreateVehicleAsync(dynamic vehicle)
    {
        try
        {
            _logger.LogInformation("Creating vehicle on server");
            
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogWarning("Not authenticated. Cannot create vehicle.");
                return null;
            }
            
            var json = JsonSerializer.Serialize(vehicle);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/vehicles", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdVehicle = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                _logger.LogInformation("Successfully created vehicle on server");
                return createdVehicle;
            }
            
            _logger.LogWarning("Failed to create vehicle. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle on server");
            return null;
        }
    }
    
    public async Task<dynamic?> GetTelemetryAsync()
    {
        try
        {
            _logger.LogInformation("Getting telemetry from server");
            
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogWarning("Not authenticated. Cannot get telemetry.");
                return null;
            }
            
            var response = await _httpClient.GetAsync("/api/telemetry/metrics");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var telemetry = JsonSerializer.Deserialize<JsonElement>(content);
                
                _logger.LogInformation("Successfully retrieved telemetry from server");
                return telemetry;
            }
            
            _logger.LogWarning("Failed to get telemetry. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting telemetry from server");
            return null;
        }
    }
    
    public async Task<bool> RecordMetricAsync(string metricName, double value, string unit = "")
    {
        try
        {
            _logger.LogInformation("Recording metric on server: {MetricName} = {Value} {Unit}", metricName, value, unit);
            
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogWarning("Not authenticated. Cannot record metric.");
                return false;
            }
            
            var metricRequest = new
            {
                MetricName = metricName,
                Value = value,
                Unit = unit,
                Tags = new Dictionary<string, string>
                {
                    ["source"] = "DotNetFeaturesSample",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            };
            
            var json = JsonSerializer.Serialize(metricRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/telemetry/metrics", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully recorded metric on server");
                return true;
            }
            
            _logger.LogWarning("Failed to record metric. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric on server");
            return false;
        }
    }
    
    public async Task<dynamic?> GetHealthAsync()
    {
        try
        {
            _logger.LogInformation("Checking server health");
            
            var response = await _httpClient.GetAsync("/api/telemetry/health");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<JsonElement>(content);
                
                _logger.LogInformation("Server health check successful");
                return health;
            }
            
            _logger.LogWarning("Server health check failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking server health");
            return null;
        }
    }
    
    public async Task<bool> TestWindowsEventLogAsync(string message)
    {
        try
        {
            _logger.LogInformation("Testing Windows Event Log feature on server");
            
            if (string.IsNullOrEmpty(_jwtToken))
            {
                _logger.LogWarning("Not authenticated. Cannot test Windows Event Log.");
                return false;
            }
            
            var eventRequest = new
            {
                Message = message,
                Level = "Information",
                EventId = 2000
            };
            
            var json = JsonSerializer.Serialize(eventRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/telemetry/windows-event", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Windows Event Log test successful. Response: {Response}", responseContent);
                return true;
            }
            
            _logger.LogWarning("Windows Event Log test failed. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Windows Event Log on server");
            return false;
        }
    }
}