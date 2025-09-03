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
    private readonly IHostApplicationLifetime _applicationLifetime;

    public DotNetFeaturesApplication(
        ILogger<DotNetFeaturesApplication> logger,
        IOptions<SampleSettings> settings,
        IPrivacyComplianceService privacyService,
        IJsonSerializationService jsonService,
        IFileCompressionService compressionService,
        IResilienceService resilienceService,
        CommandLineFeature commandLineFeature,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _settings = settings.Value;
        _privacyService = privacyService;
        _jsonService = jsonService;
        _compressionService = compressionService;
        _resilienceService = resilienceService;
        _commandLineFeature = commandLineFeature;
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
}