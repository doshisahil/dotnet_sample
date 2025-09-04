using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNetFeaturesServer;
using DotNetFeaturesServer.Data;
using DotNetFeaturesServer.Models;

namespace DotNetFeatures.Tests;

/// <summary>
/// Integration tests for the DotNet Features Server API
/// </summary>
public class ServerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ServerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database and add in-memory database for testing
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/telemetry/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Auth_Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "demo",
            Password = "Demo123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(loginResponse.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task Auth_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "invalid",
            Password = "invalid"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Vehicles_GetAll_RequiresAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/vehicles");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Vehicles_GetAll_WithValidToken_ReturnsVehicles()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/vehicles");

        // Assert
        response.EnsureSuccessStatusCode();
        var vehicles = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(vehicles);
    }

    [Fact]
    public async Task Vehicles_Create_WithValidToken_CreatesVehicle()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var vehicle = new
        {
            Type = "Car",
            Brand = "Test Brand",
            Model = "Test Model",
            Year = 2024,
            Price = 25000.00m,
            AdditionalData = "{\"testData\": true}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vehicles", vehicle);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdVehicle = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(createdVehicle.TryGetProperty("id", out var id));
        Assert.True(id.GetInt32() > 0);
    }

    [Fact]
    public async Task Telemetry_GetMetrics_WithValidToken_ReturnsMetrics()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/telemetry/metrics");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.Contains("Metrics", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Telemetry_RecordMetric_WithValidToken_RecordsMetric()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var metric = new
        {
            MetricName = "test_metric",
            Value = 123.45,
            Unit = "test_unit",
            Tags = new Dictionary<string, string> { { "source", "unit_test" } }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/telemetry/metrics", metric);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("message", out var message));
        Assert.Contains("recorded successfully", message.GetString());
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new
        {
            Username = "demo",
            Password = "Demo123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        return loginResponse.GetProperty("token").GetString()!;
    }
}