# .NET Core Features Sample

A comprehensive sample application demonstrating various powerful .NET Core features and capabilities, addressing concerns about .NET Standard limitations by showcasing the rich ecosystem of .NET Core.

## Features Demonstrated

This application showcases the following .NET Core features:

### 🏗️ Generic Host & Dependency Injection
- **Generic Host**: Complete application lifecycle management
- **Dependency Injection**: Built-in DI container with service registration
- **Hosted Services**: Background services that run throughout the application lifetime
- **Service Scopes**: Proper service lifetime management

### ⚙️ Configuration System
- **Multiple Configuration Sources**:
  - JSON files (`appsettings.json`)
  - Environment variables
  - Command line arguments
  - Azure Key Vault (concept demonstrated)
  - Azure App Configuration (concept demonstrated)
  - In-memory objects
  - Custom providers

### 📝 Structured Logging
- **Multiple Logging Providers**:
  - Console logging with timestamps
  - Debug output
  - Event Source logging
  - Event Log (Windows only)
- **Log Levels**: Trace, Debug, Information, Warning, Error, Critical
- **Structured Data**: JSON-formatted log entries with contextual information

### 🔒 Privacy & Compliance
- **Data Classification**: Identify sensitive information types
- **Data Redaction**: Automatic redaction of PII, SSN, credit card numbers, emails
- **Compliance Patterns**: Demonstrate privacy-first data handling

### 📊 JSON Serialization with Polymorphism
- **System.Text.Json**: High-performance JSON serialization
- **Polymorphic Serialization**: Handle inheritance hierarchies
- **Type Discrimination**: Automatic type resolution during deserialization
- **Custom Converters**: Flexible serialization strategies

### 📦 File Operations
- **Compression & Decompression**:
  - ZIP archive creation and extraction
  - GZip compression with size reporting
  - Batch file operations
- **Cross-platform**: Works on Windows, Linux, and macOS

### 👁️ File System Monitoring
- **FileSystemWatcher**: Real-time file and directory monitoring
- **Cross-platform**: Monitor file changes on any supported OS
- **Event Processing**: Handle Created, Changed, Deleted, and Renamed events
- **Filtering**: Watch specific file types and patterns
- **Automated Cleanup**: Intelligent file management

### 🔄 Resilience Patterns
- **Retry Pattern**: Automatic retry with exponential backoff
- **Circuit Breaker**: Fail-fast protection for downstream services
- **Timeout Pattern**: Prevent hanging operations
- **Combined Policies**: Multiple patterns working together
- **Observability**: Detailed logging of resilience events

### 🌐 WebSocket Support
- **WebSocket Server**: Full-featured WebSocket server implementation
- **Client Simulation**: Built-in client for testing
- **Real-time Communication**: Bidirectional messaging
- **Connection Management**: Handle multiple concurrent connections
- **Graceful Shutdown**: Proper cleanup of resources

### 💻 Command Line Parsing
- **System.CommandLine**: Modern command-line parsing
- **Hierarchical Commands**: Commands with subcommands
- **Strongly Typed Options**: Type-safe argument parsing
- **Help Generation**: Automatic help text generation
- **Validation**: Input validation and error handling

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Any operating system (Windows, Linux, macOS)

### Building the Application
```bash
cd DotNetFeaturesSample
dotnet build
```

### Running the Application

#### Full Feature Demonstration
```bash
dotnet run
```
This runs all features with background services (FileWatcher, WebSocket server) and demonstrates each capability.

#### Command Line Interface
```bash
# Show help
dotnet run -- --help

# Run specific feature demos
dotnet run -- demo json --count 2 --delay 500
dotnet run -- demo compression
dotnet run -- demo resilience

# File operations
dotnet run -- file compress ./data --output data.zip
dotnet run -- file watch ./watch --filter "*.txt"

# Server operations
dotnet run -- server --port 8080 --protocol websocket
```

## Project Structure

```
DotNetFeaturesSample/
├── Program.cs                          # Application entry point and host configuration
├── appsettings.json                   # Configuration settings
├── DotNetFeaturesApplication.cs       # Main orchestrator service
├── Models/
│   └── SampleModels.cs               # Data models and configuration classes
├── Services/
│   ├── PrivacyComplianceService.cs   # Data redaction and privacy features
│   ├── JsonSerializationService.cs   # Polymorphic JSON serialization
│   ├── FileCompressionService.cs     # File compression and decompression
│   ├── FileWatcherService.cs         # File system monitoring
│   ├── ResilienceService.cs          # Resilience patterns implementation
│   └── WebSocketService.cs           # WebSocket server implementation
└── Features/
    └── CommandLineFeature.cs         # Command line argument parsing
```

## Key Architectural Patterns

### Dependency Injection
All services are registered in the DI container and automatically injected:
```csharp
builder.Services.AddScoped<IPrivacyComplianceService, PrivacyComplianceService>();
builder.Services.AddHostedService<FileWatcherService>();
```

### Configuration Pattern
Settings are loaded from multiple sources and bound to strongly-typed classes:
```csharp
builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.Configure<SampleSettings>(builder.Configuration.GetSection("SampleSettings"));
```

### Hosted Services
Long-running background services integrate with the host lifetime:
```csharp
public class FileWatcherService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) { }
}
```

### Structured Logging
Rich, contextual logging throughout the application:
```csharp
_logger.LogInformation("Processing file event: {EventDetails}", eventDetails);
```

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "SampleSettings": {
    "ApplicationName": ".NET Core Features Sample",
    "Features": {
      "EnableFileWatcher": true,
      "EnableWebSocket": true,
      "WatchDirectory": "./watch",
      "WebSocketPort": 8080
    }
  }
}
```

Override settings using environment variables:
```bash
export SampleSettings__Features__WebSocketPort=9090
dotnet run
```

## Why .NET Core Over .NET Standard

This application demonstrates that .NET Core provides:

1. **Rich Runtime Features**: Full application hosting, background services, and lifecycle management
2. **Advanced Libraries**: Comprehensive packages for resilience, compliance, and real-time communication
3. **Cross-Platform Capabilities**: File system monitoring, compression, and networking that work everywhere
4. **Modern APIs**: Latest JSON serialization, logging, and configuration systems
5. **Performance**: High-performance implementations of common patterns
6. **Ecosystem**: Rich package ecosystem with Microsoft and community libraries

While .NET Standard provides portability, .NET Core offers the full power of modern application development.

## Sample Output

When you run the application, you'll see output demonstrating each feature:

```
=== .NET Core Features Sample Application ===
=== Configuration Providers Demo ===
- Application Name: .NET Core Features Sample
- Version: 1.0.0

=== Privacy/Compliance Features Demo ===
Original: John Doe, 123-45-6789, john.doe@example.com
Redacted: [REDACTED NAME], ***-**-****, ***@***.***

=== JSON Serialization with Polymorphism Demo ===
Serialized JSON:
[
  {
    "type": "Car",
    "id": "1",
    "brand": "Toyota",
    "data": { "numberOfDoors": 4 }
  }
]

=== FileSystemWatcher Demo ===
File created: ./watch/test_20250902_145151.txt
Processing file event: { "EventType": "Created", "FilePath": "...", ... }

=== Resilience Patterns Demo ===
Retry attempt 1 for operation due to: Simulated failure on attempt 1
Circuit breaker opened due to 3 consecutive failures
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Feel free to submit issues and enhancement requests! This sample is designed to be educational and demonstrate best practices in .NET Core development.