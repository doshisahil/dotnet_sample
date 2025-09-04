using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetFeaturesSample;
using DotNetFeaturesSample.Models;
using DotNetFeaturesSample.Services;
using DotNetFeaturesSample.Features;

// Create the Generic Host with all .NET Core features configured
var builder = Host.CreateApplicationBuilder(args);

// Configuration: Add multiple configuration providers
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// Configure settings
builder.Services.Configure<SampleSettings>(builder.Configuration.GetSection("SampleSettings"));

// Logging: Configure multiple logging providers
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventSourceLogger();
    
    // Configure log levels
    logging.SetMinimumLevel(LogLevel.Information);
});

// Dependency Injection: Register all services
builder.Services.AddScoped<IPrivacyComplianceService, PrivacyComplianceService>();
builder.Services.AddScoped<IJsonSerializationService, JsonSerializationService>();
builder.Services.AddScoped<IFileCompressionService, FileCompressionService>();
builder.Services.AddScoped<IResilienceService, ResilienceService>();
builder.Services.AddScoped<CommandLineFeature>();

// HTTP Client for server communication
builder.Services.AddHttpClient<IApiClientService, ApiClientService>();
builder.Services.AddScoped<IApiClientService, ApiClientService>();

// Register hosted services (Background Services)
builder.Services.AddHostedService<DotNetFeaturesApplication>();
builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddHostedService<WebSocketService>();

var host = builder.Build();

try
{
    // Get logger for startup
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("=== .NET Core Features Sample Application ===");
    logger.LogInformation("This application demonstrates various .NET Core features:");
    logger.LogInformation("- Generic Host with Dependency Injection");
    logger.LogInformation("- Configuration from multiple sources");
    logger.LogInformation("- Structured logging with multiple providers");
    logger.LogInformation("- Privacy/compliance with data redaction");
    logger.LogInformation("- JSON serialization with polymorphism");
    logger.LogInformation("- File compression and decompression");
    logger.LogInformation("- FileSystemWatcher for hot folder monitoring");
    logger.LogInformation("- Resilience patterns (retry, circuit breaker, etc.)");
    logger.LogInformation("- WebSocket server support");
    logger.LogInformation("- Command line argument parsing");
    logger.LogInformation("");
    
    // If command line arguments are provided, handle them
    if (args.Length > 0)
    {
        logger.LogInformation("Processing command line arguments...");
        var commandLineFeature = host.Services.GetRequiredService<CommandLineFeature>();
        var exitCode = await commandLineFeature.ParseAndExecuteAsync(args);
        return exitCode;
    }
    
    // Otherwise run the full application
    logger.LogInformation("Starting application with all features...");
    await host.RunAsync();
    
    return 0;
}
catch (Exception ex)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Application terminated unexpectedly");
    return 1;
}
