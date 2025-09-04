using System.ComponentModel.DataAnnotations;

namespace DotNetFeaturesServer.Models;

/// <summary>
/// User model for authentication and API access
/// </summary>
public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<ApiLog> ApiLogs { get; set; } = new List<ApiLog>();
}

/// <summary>
/// Vehicle model for demonstrating database operations
/// </summary>
public class Vehicle
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // Car, Motorcycle, etc.
    
    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;
    
    public int Year { get; set; }
    
    public decimal Price { get; set; }
    
    public string AdditionalData { get; set; } = string.Empty; // JSON for type-specific data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
}

/// <summary>
/// API log for telemetry and monitoring
/// </summary>
public class ApiLog
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public string Endpoint { get; set; } = string.Empty;
    
    public string Method { get; set; } = string.Empty;
    
    public int StatusCode { get; set; }
    
    public long DurationMs { get; set; }
    
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    
    public string RequestBody { get; set; } = string.Empty;
    
    public string ResponseBody { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = string.Empty;
    
    public string IpAddress { get; set; } = string.Empty;
    
    public User User { get; set; } = null!;
}

/// <summary>
/// Performance metric for custom telemetry
/// </summary>
public class PerformanceMetric
{
    public int Id { get; set; }
    
    public string MetricName { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    public string Unit { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string Tags { get; set; } = string.Empty; // JSON for additional tags
}