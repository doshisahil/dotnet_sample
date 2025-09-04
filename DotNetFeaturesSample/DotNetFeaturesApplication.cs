using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotNetFeaturesSample.Models;
using DotNetFeaturesSample.Services;
using DotNetFeaturesSample.Features;

namespace DotNetFeaturesSample;

/// <summary>
/// Main application service that orchestrates all .NET Core feature demonstrations
/// </summary>
public class DotNetFeaturesApplication : BackgroundService
{
    private readonly ILogger<DotNetFeaturesApplication> _logger;
    private readonly SampleSettings _settings;
    private readonly IPrivacyComplianceService _privacyService;
    private readonly IJsonSerializationService _jsonService;
    private readonly IFileCompressionService _compressionService;
    private readonly IResilienceService _resilienceService;
    private readonly CommandLineFeature _commandLineFeature;
    private readonly IApiClientService _apiClientService;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public DotNetFeaturesApplication(
        ILogger<DotNetFeaturesApplication> logger,
        IOptions<SampleSettings> settings,
        IPrivacyComplianceService privacyService,
        IJsonSerializationService jsonService,
        IFileCompressionService compressionService,
        IResilienceService resilienceService,
        CommandLineFeature commandLineFeature,
        IApiClientService apiClientService,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _settings = settings.Value;
        _privacyService = privacyService;
        _jsonService = jsonService;
        _compressionService = compressionService;
        _resilienceService = resilienceService;
        _commandLineFeature = commandLineFeature;
        _apiClientService = apiClientService;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting {ApplicationName} v{Version}", 
            _settings.ApplicationName, _settings.Version);

        try
        {
            // Give time for other hosted services to start
            await Task.Delay(2000, stoppingToken);

            // Demonstrate all features
            await DemonstrateAllFeaturesAsync();

            // Keep running for a bit to let background services work
            _logger.LogInformation("All demonstrations completed. Background services will continue running...");
            _logger.LogInformation("Press Ctrl+C to stop the application");

            await Task.Delay(30000, stoppingToken); // Run for 30 seconds
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Application stopped by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application error");
        }
        finally
        {
            _logger.LogInformation("Shutting down application...");
            _applicationLifetime.StopApplication();
        }
    }

    private async Task DemonstrateAllFeaturesAsync()
    {
        _logger.LogInformation("=== Starting .NET Core Features Demonstration ===");

        // 1. Configuration demonstration
        await DemonstrateConfigurationAsync();

        // 2. Dependency Injection demonstration
        await DemonstrateDependencyInjectionAsync();

        // 3. Logging demonstration
        await DemonstrateLoggingAsync();

        // 4. Privacy/Compliance demonstration
        await DemonstratePrivacyComplianceAsync();

        // 5. JSON Serialization with Polymorphism
        await DemonstrateJsonSerializationAsync();

        // 6. File Compression
        await DemonstrateFileCompressionAsync();

        // 7. Resilience Patterns
        await DemonstrateResiliencePatternsAsync();

        // 8. Command Line Parsing
        await DemonstrateCommandLineParsingAsync();

        // 9. Server API Integration
        await DemonstrateServerApiIntegrationAsync();

        _logger.LogInformation("=== All Feature Demonstrations Completed ===");
    }

    private async Task DemonstrateConfigurationAsync()
    {
        _logger.LogInformation("=== Configuration Providers Demo ===");
        
        _logger.LogInformation("Application Settings:");
        _logger.LogInformation("- Application Name: {ApplicationName}", _settings.ApplicationName);
        _logger.LogInformation("- Version: {Version}", _settings.Version);
        _logger.LogInformation("- File Watcher Enabled: {FileWatcherEnabled}", _settings.Features.EnableFileWatcher);
        _logger.LogInformation("- WebSocket Enabled: {WebSocketEnabled}", _settings.Features.EnableWebSocket);
        _logger.LogInformation("- Watch Directory: {WatchDirectory}", _settings.Features.WatchDirectory);
        _logger.LogInformation("- WebSocket Port: {WebSocketPort}", _settings.Features.WebSocketPort);

        // Demonstrate reading from different configuration sources
        _logger.LogInformation("Configuration can be loaded from:");
        _logger.LogInformation("- appsettings.json files");
        _logger.LogInformation("- Environment variables");
        _logger.LogInformation("- Command line arguments");
        _logger.LogInformation("- Azure Key Vault");
        _logger.LogInformation("- Azure App Configuration");
        _logger.LogInformation("- In-memory objects");
        _logger.LogInformation("- Custom providers");

        await Task.CompletedTask;
    }

    private async Task DemonstrateDependencyInjectionAsync()
    {
        _logger.LogInformation("=== Dependency Injection Demo ===");
        
        _logger.LogInformation("All services in this application are registered with the DI container:");
        _logger.LogInformation("- IPrivacyComplianceService -> PrivacyComplianceService (Scoped)");
        _logger.LogInformation("- IJsonSerializationService -> JsonSerializationService (Scoped)");
        _logger.LogInformation("- IFileCompressionService -> FileCompressionService (Scoped)");
        _logger.LogInformation("- IResilienceService -> ResilienceService (Scoped)");
        _logger.LogInformation("- CommandLineFeature (Scoped)");
        _logger.LogInformation("- Background Services: FileWatcherService, WebSocketService");

        _logger.LogInformation("Services are automatically injected into constructors and resolved by the DI container");
        
        await Task.CompletedTask;
    }

    private async Task DemonstrateLoggingAsync()
    {
        _logger.LogInformation("=== Logging Providers Demo ===");
        
        _logger.LogTrace("This is a TRACE level message");
        _logger.LogDebug("This is a DEBUG level message");
        _logger.LogInformation("This is an INFORMATION level message");
        _logger.LogWarning("This is a WARNING level message");
        _logger.LogError("This is an ERROR level message");
        _logger.LogCritical("This is a CRITICAL level message");

        _logger.LogInformation("Supported logging providers:");
        _logger.LogInformation("- Console (currently active)");
        _logger.LogInformation("- Debug");
        _logger.LogInformation("- EventSource");
        _logger.LogInformation("- EventLog (Windows only)");
        _logger.LogInformation("- File (with third-party providers)");
        _logger.LogInformation("- Structured logging (Serilog, NLog, etc.)");

        await Task.CompletedTask;
    }

    private async Task DemonstratePrivacyComplianceAsync()
    {
        _logger.LogInformation("=== Privacy/Compliance Features Demo ===");
        
        var personalInfo = new PersonalInfo
        {
            Name = "John Doe",
            SocialSecurityNumber = "123-45-6789",
            Email = "john.doe@example.com",
            CreditCardNumber = "4111-1111-1111-1111",
            DateOfBirth = new DateTime(1990, 5, 15)
        };

        var redactionResult = await _privacyService.RedactSensitiveDataAsync(personalInfo);
        _logger.LogInformation("Redaction Result:\\n{Result}", redactionResult);

        await _privacyService.ClassifyAndRedactAsync("This text contains sensitive information like SSN 123-45-6789");
    }

    private async Task DemonstrateJsonSerializationAsync()
    {
        _logger.LogInformation("=== JSON Serialization with Polymorphism Demo ===");
        
        await _jsonService.DemonstratePolymorphismAsync();
    }

    private async Task DemonstrateFileCompressionAsync()
    {
        _logger.LogInformation("=== File Compression and Decompression Demo ===");
        
        await _compressionService.DemonstrateCompressionAsync();
    }

    private async Task DemonstrateResiliencePatternsAsync()
    {
        _logger.LogInformation("=== Resilience Patterns Demo ===");
        
        await _resilienceService.DemonstrateResiliencePatternsAsync();
    }

    private async Task DemonstrateCommandLineParsingAsync()
    {
        _logger.LogInformation("=== Command Line Argument Parsing Demo ===");
        
        _commandLineFeature.DemonstrateCommandLineParsing();
        
        await Task.CompletedTask;
    }

    private async Task DemonstrateServerApiIntegrationAsync()
    {
        _logger.LogInformation("=== Server API Integration Demo ===");
        _logger.LogInformation("Demonstrating OAuth, REST API calls, telemetry, and .NET 8 features");

        try
        {
            // 1. Health Check
            _logger.LogInformation("1. Checking server health...");
            var health = await _apiClientService.GetHealthAsync();
            if (health != null)
            {
                _logger.LogInformation("✓ Server is healthy");
            }
            else
            {
                _logger.LogWarning("⚠ Server health check failed - continuing with demo");
            }

            // 2. Authentication (OAuth demonstration)
            _logger.LogInformation("2. Authenticating with server using OAuth/JWT...");
            var authenticated = await _apiClientService.AuthenticateAsync("demo", "Demo123!");
            
            if (!authenticated)
            {
                _logger.LogWarning("⚠ Authentication failed - server may not be running");
                _logger.LogInformation("To start the server, run: cd DotNetFeaturesServer && dotnet run");
                return;
            }
            
            _logger.LogInformation("✓ Successfully authenticated with JWT token");

            // 3. Demonstrate REST API calls
            _logger.LogInformation("3. Demonstrating REST API calls...");
            
            // Get existing vehicles
            var vehicles = await _apiClientService.GetVehiclesAsync();
            if (vehicles != null)
            {
                _logger.LogInformation("✓ Retrieved vehicles from server");
                var vehicleList = vehicles.ToList();
                var count = Math.Min(3, vehicleList.Count);
                _logger.LogInformation("  - Showing first {Count} vehicles", count);
                for (int i = 0; i < count; i++)
                {
                    _logger.LogInformation("  - Vehicle {Index}: Available", i + 1);
                }
            }

            // Create a new vehicle
            var newVehicle = new
            {
                Type = "Car",
                Brand = "Tesla",
                Model = "Model 3",
                Year = 2024,
                Price = 45000.00m,
                AdditionalData = "{\"batteryRange\": 358, \"autopilot\": true}"
            };

            var createdVehicle = await _apiClientService.CreateVehicleAsync(newVehicle);
            if (createdVehicle != null)
            {
                _logger.LogInformation("✓ Successfully created new vehicle on server");
            }

            // 4. Demonstrate Telemetry
            _logger.LogInformation("4. Demonstrating telemetry and metrics...");
            
            // Record custom metrics
            await _apiClientService.RecordMetricAsync("client_demo_execution", 1, "count");
            await _apiClientService.RecordMetricAsync("api_integration_test", DateTime.UtcNow.Millisecond, "ms");
            
            // Get telemetry data
            var telemetry = await _apiClientService.GetTelemetryAsync();
            if (telemetry != null)
            {
                _logger.LogInformation("✓ Retrieved telemetry data from server");
            }

            // 5. Demonstrate Windows Event Log feature
            _logger.LogInformation("5. Testing Windows Event Log integration...");
            var eventLogSuccess = await _apiClientService.TestWindowsEventLogAsync(
                "DotNet Features Sample successfully connected to server and demonstrated .NET 8 features!");
            
            if (eventLogSuccess)
            {
                _logger.LogInformation("✓ Windows Event Log demonstration completed");
                _logger.LogInformation("  Check Windows Event Viewer > Application Log for the logged event");
            }

            _logger.LogInformation("=== Server API Integration Demo Completed Successfully ===");
            _logger.LogInformation("Demonstrated features:");
            _logger.LogInformation("  ✓ OAuth/JWT Authentication");
            _logger.LogInformation("  ✓ REST API calls (GET, POST)");
            _logger.LogInformation("  ✓ Database operations via API");
            _logger.LogInformation("  ✓ Custom telemetry and metrics");
            _logger.LogInformation("  ✓ Distributed caching");
            _logger.LogInformation("  ✓ Windows Event Log integration");
            _logger.LogInformation("  ✓ Structured logging and monitoring");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during server API integration demonstration");
            _logger.LogInformation("Make sure the DotNetFeaturesServer is running on https://localhost:7000");
        }
    }
}