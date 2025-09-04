using Microsoft.EntityFrameworkCore;
using DotNetFeaturesServer.Models;

namespace DotNetFeaturesServer.Data;

/// <summary>
/// Entity Framework Core database context for the application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ApiLog> ApiLogs { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
        
        // Vehicle configuration
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
        
        // ApiLog configuration
        modelBuilder.Entity<ApiLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ApiLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RequestTime).HasDefaultValueSql("GETUTCDATE()");
        });
        
        // PerformanceMetric configuration
        modelBuilder.Entity<PerformanceMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });
        
        // Seed data
        SeedData(modelBuilder);
    }
    
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "demo",
                Email = "demo@dotnetfeatures.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        );
        
        // Seed sample vehicles
        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle
            {
                Id = 1,
                Type = "Car",
                Brand = "Toyota",
                Model = "Camry",
                Year = 2023,
                Price = 28000.00m,
                AdditionalData = "{\"numberOfDoors\": 4, \"fuelType\": \"Hybrid\"}",
                UserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Vehicle
            {
                Id = 2,
                Type = "Motorcycle",
                Brand = "Harley-Davidson",
                Model = "Street 750",
                Year = 2023,
                Price = 7599.00m,
                AdditionalData = "{\"hasSidecar\": false, \"engineSize\": 750}",
                UserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}