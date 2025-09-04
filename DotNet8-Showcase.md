# .NET 8 Features Showcase: Complete Enterprise Application

This repository demonstrates the incredible ease and power of modern .NET development through a comprehensive example that includes a console application, ASP.NET Core Web API server, OAuth authentication, telemetry, database operations, caching, unit testing, and benchmarking.

## 🚀 Why .NET 8+ is Amazing for Developers

.NET 8 represents a quantum leap in developer productivity, offering enterprise-grade features with minimal configuration. This showcase proves that building sophisticated applications with authentication, monitoring, and high-performance operations is incredibly straightforward in the .NET ecosystem.

## 📚 Table of Contents

- [Architecture Overview](#architecture-overview)
- [Key Features Demonstrated](#key-features-demonstrated)
- [Projects Structure](#projects-structure)
- [Getting Started](#getting-started)
- [Feature Deep Dive](#feature-deep-dive)
- [Performance & Benchmarking](#performance--benchmarking)
- [Testing Strategy](#testing-strategy)
- [Deployment & Production](#deployment--production)
- [Developer Experience Highlights](#developer-experience-highlights)

## 🏗️ Architecture Overview

This solution demonstrates a modern microservices-style architecture where a console application communicates with an ASP.NET Core Web API server:

```
┌─────────────────────┐    HTTP/REST     ┌─────────────────────┐
│  Console Client     │ ◄──────────────► │   ASP.NET Server    │
│  (DotNetFeatures    │   OAuth + JWT    │  (DotNetFeatures    │
│   Sample)           │                  │   Server)           │
└─────────────────────┘                  └─────────────────────┘
         │                                         │
         │                                         │
         ▼                                         ▼
┌─────────────────────┐                  ┌─────────────────────┐
│   Unit Tests &      │                  │   Database &        │
│   Benchmarks        │                  │   Caching           │
└─────────────────────┘                  └─────────────────────┘
```

## ✨ Key Features Demonstrated

### 🔐 Authentication & Security
- **JWT Token Authentication**: Industry-standard OAuth implementation
- **Password Hashing**: Secure BCrypt password storage
- **Authorization Middleware**: Attribute-based authorization

### 📊 Observability & Telemetry
- **OpenTelemetry Integration**: Distributed tracing and metrics
- **Custom Metrics**: Business-specific performance indicators
- **Structured Logging**: JSON-formatted logs with correlation IDs
- **Windows Event Log**: Native OS-level logging integration
- **Health Checks**: Built-in endpoint monitoring

### 🗄️ Data Management
- **Entity Framework Core**: Code-first database development
- **In-Memory Database**: Perfect for development and testing
- **CRUD Operations**: Complete REST API implementation
- **Data Validation**: Comprehensive input validation

### ⚡ Performance & Caching
- **Memory Caching**: High-performance in-memory caching
- **Cache Statistics**: Real-time cache performance monitoring
- **Async/Await**: Non-blocking operations throughout

### 🧪 Testing & Quality
- **Unit Tests**: Comprehensive xUnit test suite
- **Integration Tests**: End-to-end API testing
- **Performance Benchmarks**: BenchmarkDotNet integration
- **Mocking**: Moq framework for isolated testing

### 🛠️ Developer Experience
- **Dependency Injection**: Built-in DI container
- **Configuration System**: Multiple configuration sources
- **Hot Reload**: Development-time productivity features
- **Swagger/OpenAPI**: Interactive API documentation

## 📁 Projects Structure

```
├── DotNetFeaturesSample/           # Console application (client)
│   ├── Program.cs                  # Application entry point
│   ├── Services/                   # Business logic services
│   │   ├── ApiClientService.cs     # HTTP client for server communication
│   │   ├── JsonSerializationService.cs
│   │   ├── FileCompressionService.cs
│   │   ├── ResilienceService.cs
│   │   └── WebSocketService.cs
│   └── Models/                     # Data models
│
├── DotNetFeaturesServer/           # ASP.NET Core Web API (server)
│   ├── Program.cs                  # Server configuration
│   ├── Controllers/                # REST API endpoints
│   │   ├── AuthController.cs       # Authentication endpoints
│   │   ├── VehiclesController.cs   # CRUD operations
│   │   └── TelemetryController.cs  # Monitoring endpoints
│   ├── Services/                   # Business services
│   │   ├── JwtService.cs           # JWT token management
│   │   ├── AuthService.cs          # User authentication
│   │   ├── VehicleService.cs       # Business logic
│   │   ├── CacheService.cs         # Caching implementation
│   │   └── TelemetryService.cs     # Metrics and monitoring
│   ├── Data/                       # Database context
│   └── Models/                     # Entity models
│
├── DotNetFeatures.Tests/           # Unit and integration tests
│   ├── ServerIntegrationTests.cs  # API integration tests
│   └── ClientServicesTests.cs     # Unit tests
│
└── DotNetFeatures.Benchmarks/     # Performance benchmarks
    └── Program.cs                  # BenchmarkDotNet suite
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Any IDE (Visual Studio, VS Code, Rider)
- Any operating system (Windows, Linux, macOS)

### Quick Start

1. **Clone and Build**
```bash
git clone https://github.com/doshisahil/dotnet_sample.git
cd dotnet_sample
dotnet build
```

2. **Start the Server**
```bash
cd DotNetFeaturesServer
dotnet run
```
The server will start on https://localhost:7000 with Swagger UI available at the root.

3. **Run the Client (in a new terminal)**
```bash
cd DotNetFeaturesSample
dotnet run
```

4. **Run Tests**
```bash
dotnet test DotNetFeatures.Tests
```

5. **Run Benchmarks**
```bash
cd DotNetFeatures.Benchmarks
dotnet run -c Release
```

## 🔍 Feature Deep Dive

### OAuth Authentication Made Simple

Implementing secure authentication in .NET 8 requires minimal code:

```csharp
// JWT Configuration (Program.cs)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true
        };
    });

// Protected endpoint
[Authorize]
[HttpGet]
public async Task<IActionResult> GetProtectedData() => Ok(data);
```

### Windows Event Log Integration

Logging to Windows Event Viewer is incredibly simple:

```csharp
// One line to add Windows Event Log provider
if (OperatingSystem.IsWindows()) {
    builder.Logging.AddEventLog(settings => {
        settings.SourceName = "DotNetFeaturesServer";
    });
}

// Usage anywhere in your application
_logger.LogInformation("This message appears in Windows Event Viewer!");
```

### Telemetry & Monitoring

OpenTelemetry integration provides enterprise-grade observability:

```csharp
// Add comprehensive telemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

### Database Operations with EF Core

Entity Framework Core provides seamless database operations:

```csharp
// Define models
public class Vehicle {
    public int Id { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
}

// Add to DI container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("DemoDb")); // Or SQL Server, PostgreSQL, etc.

// Use in controllers
[HttpGet]
public async Task<IActionResult> GetVehicles() =>
    Ok(await _context.Vehicles.ToListAsync());
```

### High-Performance Caching

Built-in caching with minimal configuration:

```csharp
// Register caching
builder.Services.AddMemoryCache();

// Use in services
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem) {
    if (_cache.TryGetValue(key, out T cached)) return cached;
    
    var item = await getItem();
    _cache.Set(key, item, TimeSpan.FromMinutes(30));
    return item;
}
```

## 📈 Performance & Benchmarking

The included benchmarks demonstrate .NET 8's exceptional performance:

### Sample Benchmark Results

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| JsonSerialization_SerializeVehicles | 234.5 μs | 4.2 μs | 3.9 μs | 85 KB |
| ResiliencePattern_RetrySuccess | 1.8 μs | 0.03 μs | 0.03 μs | 96 B |
| FileCompression_CreateZip | 15.2 ms | 0.28 ms | 0.26 ms | 125 KB |
| SystemTextJson_Serialize | 1.2 μs | 0.02 μs | 0.02 μs | 288 B |

### Running Benchmarks

```bash
cd DotNetFeatures.Benchmarks
dotnet run -c Release
```

## 🧪 Testing Strategy

### Integration Tests
- Full HTTP client testing against real API
- In-memory database for isolated testing
- Authentication flow testing

### Unit Tests
- Service layer testing with mocking
- Business logic validation
- Error handling verification

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "ClassName.TestMethodName"
```

## 🚀 Deployment & Production

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "DotNetFeaturesServer.dll"]
```

### Configuration for Production
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql-server;Database=DotNetFeatures;..."
  },
  "Jwt": {
    "SecretKey": "${JWT_SECRET_FROM_KEYVAULT}",
    "Issuer": "production-issuer"
  }
}
```

## 💎 Developer Experience Highlights

### What Makes .NET 8 Extraordinary

1. **Minimal API Surface**: Complex features with simple APIs
2. **Built-in Best Practices**: Security, performance, and monitoring out-of-the-box
3. **Excellent Tooling**: IntelliSense, debugging, and hot reload
4. **Cross-Platform**: Run anywhere without changes
5. **Performance**: Industry-leading performance with minimal effort
6. **Ecosystem**: Rich package ecosystem with NuGet

### Code Examples That Show .NET's Elegance

#### Adding Health Checks
```csharp
// One line to add comprehensive health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Use built-in endpoint
app.MapHealthChecks("/health");
```

#### Resilience Patterns
```csharp
// Built-in retry with exponential backoff
public async Task<string> ExecuteWithRetryAsync(Func<Task<string>> operation) {
    for (int attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            return await operation();
        }
        catch (Exception) when (attempt < maxRetries) {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    throw new InvalidOperationException("All retries exhausted");
}
```

#### Configuration Binding
```csharp
// Automatically bind configuration to strongly-typed objects
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// Use anywhere via dependency injection
public class AuthService {
    public AuthService(IOptions<JwtSettings> jwtOptions) {
        _jwtSettings = jwtOptions.Value;
    }
}
```

## 🎯 Key Takeaways

This showcase demonstrates that .NET 8 provides:

1. **Enterprise Features**: Authentication, monitoring, caching, and databases work seamlessly together
2. **Developer Productivity**: Complex scenarios implemented with minimal, readable code
3. **Performance**: Excellent performance characteristics out-of-the-box
4. **Testing**: First-class testing support with comprehensive tooling
5. **Deployment**: Simple deployment to any platform or cloud

### Why Choose .NET 8 for Your Next Project

- **Rapid Development**: Build sophisticated applications quickly
- **Enterprise Ready**: Production-grade features included
- **Future Proof**: Active development with regular updates
- **Cost Effective**: Free, open-source with commercial support available
- **Versatile**: From console apps to microservices to web applications

## 🔗 Additional Resources

- [Official .NET Documentation](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Ready to experience the power of .NET 8?** Clone this repository and see how easy it is to build enterprise-grade applications with modern .NET! 🚀