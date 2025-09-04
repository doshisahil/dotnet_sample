using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using DotNetFeaturesSample.Services;
using DotNetFeaturesSample.Models;

namespace DotNetFeatures.Benchmarks;

/// <summary>
/// Benchmarks for .NET Features performance testing
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<DotNetFeaturesBenchmark>();
    }
}

[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
public class DotNetFeaturesBenchmark
{
    private JsonSerializationService _jsonService = null!;
    private FileCompressionService _compressionService = null!;
    private ResilienceService _resilienceService = null!;
    private List<Vehicle> _vehicles = null!;
    private string _tempDirectory = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup logging
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Initialize services
        _jsonService = new JsonSerializationService(loggerFactory.CreateLogger<JsonSerializationService>());
        _compressionService = new FileCompressionService(loggerFactory.CreateLogger<FileCompressionService>());
        _resilienceService = new ResilienceService(loggerFactory.CreateLogger<ResilienceService>());

        // Setup test data
        _vehicles = new List<Vehicle>();
        for (int i = 0; i < 1000; i++)
        {
            if (i % 2 == 0)
            {
                _vehicles.Add(new Car
                {
                    Id = i.ToString(),
                    Brand = $"Brand{i}",
                    NumberOfDoors = 4
                });
            }
            else
            {
                _vehicles.Add(new Motorcycle
                {
                    Id = i.ToString(),
                    Brand = $"Brand{i}",
                    HasSidecar = i % 3 == 0
                });
            }
        }

        _tempDirectory = Path.Combine(Path.GetTempPath(), "benchmark_test");
        Directory.CreateDirectory(_tempDirectory);
        
        // Create test files
        for (int i = 0; i < 100; i++)
        {
            File.WriteAllText(Path.Combine(_tempDirectory, $"file{i}.txt"), $"Content for file {i}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Benchmark]
    public async Task<string> JsonSerialization_SerializeVehicles()
    {
        return await _jsonService.SerializeVehiclesAsync(_vehicles);
    }

    [Benchmark]
    public async Task<List<Vehicle>?> JsonSerialization_DeserializeVehicles()
    {
        var json = await _jsonService.SerializeVehiclesAsync(_vehicles);
        return await _jsonService.DeserializeVehiclesAsync(json);
    }

    [Benchmark]
    public async Task<string> ResiliencePattern_RetrySuccess()
    {
        return await _resilienceService.ExecuteWithRetryAsync(async () =>
        {
            await Task.Delay(1); // Simulate work
            return "Success";
        });
    }

    [Benchmark]
    public async Task<string> ResiliencePattern_RetryWithFailure()
    {
        var attemptCount = 0;
        return await _resilienceService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            await Task.Delay(1); // Simulate work
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("Simulated failure");
            }
            return "Success after retry";
        });
    }

    [Benchmark]
    public async Task<DotNetFeaturesSample.Models.FileOperationResult> FileCompression_CreateZip()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.zip");
        try
        {
            return await _compressionService.CreateZipArchiveAsync(_tempDirectory, zipPath);
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }

    [Benchmark]
    public string SystemTextJson_Serialize()
    {
        var data = new
        {
            Id = 123,
            Name = "Test Object",
            Values = Enumerable.Range(1, 100).ToArray(),
            Timestamp = DateTime.UtcNow
        };
        
        return JsonSerializer.Serialize(data);
    }

    [Benchmark]
    public async Task FileIO_WriteAndRead()
    {
        var testFile = Path.Combine(_tempDirectory, $"benchmark_{Guid.NewGuid()}.txt");
        var content = "This is a benchmark test content with some data to write and read.";
        
        try
        {
            await File.WriteAllTextAsync(testFile, content);
            var readContent = await File.ReadAllTextAsync(testFile);
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Benchmark]
    public async Task ParallelProcessing_TaskRun()
    {
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            await Task.Delay(1);
            return i * i;
        });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MemoryOperations_ArrayManipulation()
    {
        var data = new int[10000];
        
        // Fill array
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }

        // Process array
        var sum = data.Sum();
        var average = data.Average();
        var max = data.Max();

        await Task.CompletedTask;
    }
}
