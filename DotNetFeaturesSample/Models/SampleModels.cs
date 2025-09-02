namespace DotNetFeaturesSample.Models;

/// <summary>
/// Configuration settings for the application
/// </summary>
public class SampleSettings
{
    public string ApplicationName { get; set; } = "";
    public string Version { get; set; } = "";
    public FeaturesConfig Features { get; set; } = new();
}

public class FeaturesConfig
{
    public bool EnableFileWatcher { get; set; }
    public bool EnableWebSocket { get; set; }
    public string WatchDirectory { get; set; } = "";
    public int WebSocketPort { get; set; }
}

/// <summary>
/// Sample data model for demonstrating polymorphic JSON serialization
/// </summary>
public abstract class Vehicle
{
    public string Id { get; set; } = "";
    public string Brand { get; set; } = "";
    public abstract string VehicleType { get; }
}

public class Car : Vehicle
{
    public override string VehicleType => "Car";
    public int NumberOfDoors { get; set; }
}

public class Motorcycle : Vehicle
{
    public override string VehicleType => "Motorcycle";
    public bool HasSidecar { get; set; }
}

/// <summary>
/// Sample model for demonstrating privacy compliance features
/// </summary>
public class PersonalInfo
{
    public string Name { get; set; } = "";
    public string SocialSecurityNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string CreditCardNumber { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
}

/// <summary>
/// Sample model for file operations
/// </summary>
public class FileOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}