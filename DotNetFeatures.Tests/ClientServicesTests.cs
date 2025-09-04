using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using DotNetFeaturesSample.Services;

namespace DotNetFeatures.Tests;

/// <summary>
/// Unit tests for the client services
/// </summary>
public class ClientServicesTests
{
    [Fact]
    public void JsonSerializationService_SerializeVehicles_ReturnsValidJson()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonSerializationService>>();
        var service = new JsonSerializationService(mockLogger.Object);
        
        var vehicles = new List<DotNetFeaturesSample.Models.Vehicle>
        {
            new DotNetFeaturesSample.Models.Car { Id = "1", Brand = "Toyota", NumberOfDoors = 4 },
            new DotNetFeaturesSample.Models.Motorcycle { Id = "2", Brand = "Harley", HasSidecar = false }
        };

        // Act
        var result = service.SerializeVehiclesAsync(vehicles).Result;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Toyota", result);
        Assert.Contains("Harley", result);
    }

    [Fact]
    public void FileCompressionService_CreateZipArchive_CreatesArchive()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FileCompressionService>>();
        var service = new FileCompressionService(mockLogger.Object);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test_compression");
        var zipPath = Path.Combine(Path.GetTempPath(), "test.zip");

        try
        {
            // Act
            var result = service.CreateZipArchiveAsync(tempDir, zipPath).Result;

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(zipPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(zipPath)) File.Delete(zipPath);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResilienceService_ExecuteWithRetry_RetriesOnFailure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ResilienceService>>();
        var service = new ResilienceService(mockLogger.Object);
        
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Test failure");
            }
            return Task.FromResult("Success");
        };

        // Act
        var result = service.ExecuteWithRetryAsync(operation).Result;

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public void ApiClientService_Constructor_SetsDefaultHeaders()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ApiClientService>>();
        var mockConfiguration = new Mock<IConfiguration>();
        
        mockConfiguration.Setup(x => x["ServerApi:BaseUrl"]).Returns("https://test.example.com");

        using var httpClient = new HttpClient();

        // Act
        var service = new ApiClientService(httpClient, mockLogger.Object, mockConfiguration.Object);

        // Assert
        Assert.Equal(new Uri("https://test.example.com"), httpClient.BaseAddress);
        Assert.Contains("DotNetFeaturesSample", httpClient.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public void PrivacyComplianceService_RedactSensitiveData_RedactsCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PrivacyComplianceService>>();
        var service = new PrivacyComplianceService(mockLogger.Object);
        
        var personalInfo = new DotNetFeaturesSample.Models.PersonalInfo
        {
            Name = "John Doe",
            SocialSecurityNumber = "123-45-6789",
            Email = "john.doe@example.com",
            CreditCardNumber = "4111-1111-1111-1111",
            DateOfBirth = DateTime.Parse("1990-01-01")
        };

        // Act
        var result = service.RedactSensitiveDataAsync(personalInfo).Result;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("***", result);
        Assert.DoesNotContain("123-45-6789", result);
        Assert.DoesNotContain("4111-1111-1111-1111", result);
    }
}