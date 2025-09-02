using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotNetFeaturesSample.Models;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service demonstrating FileSystemWatcher for hot folder monitoring (cross-platform)
/// </summary>
public interface IFileWatcherService
{
    Task StartWatchingAsync(string directoryPath);
    Task StopWatchingAsync();
    bool IsWatching { get; }
}

public class FileWatcherService : BackgroundService, IFileWatcherService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly SampleSettings _settings;
    private FileSystemWatcher? _fileWatcher;
    private string _watchDirectory = "";

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        IOptions<SampleSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public bool IsWatching => _fileWatcher?.EnableRaisingEvents == true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.Features.EnableFileWatcher)
        {
            var watchDir = Path.GetFullPath(_settings.Features.WatchDirectory);
            await StartWatchingAsync(watchDir);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                
                // Demonstrate creating a file to trigger the watcher
                if (IsWatching && Directory.Exists(_watchDirectory))
                {
                    var testFile = Path.Combine(_watchDirectory, $"test_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    try
                    {
                        await File.WriteAllTextAsync(testFile, $"Test file created at {DateTime.Now}", stoppingToken);
                        _logger.LogInformation("Created test file: {TestFile}", testFile);
                        
                        // Clean up old test files (keep only last 3)
                        await CleanupOldTestFilesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create test file");
                    }
                }
            }
        }
    }

    public async Task StartWatchingAsync(string directoryPath)
    {
        _logger.LogInformation("Starting file system watcher for directory: {DirectoryPath}", directoryPath);

        try
        {
            // Ensure directory exists
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("Created watch directory: {DirectoryPath}", directoryPath);
            }

            _watchDirectory = directoryPath;

            // Dispose existing watcher if any
            _fileWatcher?.Dispose();

            // Create new file watcher
            _fileWatcher = new FileSystemWatcher(directoryPath)
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            // Subscribe to events
            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Error += OnError;

            _logger.LogInformation("File system watcher started successfully for: {DirectoryPath}", directoryPath);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file system watcher");
            throw;
        }
    }

    public async Task StopWatchingAsync()
    {
        _logger.LogInformation("Stopping file system watcher");

        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }

        _logger.LogInformation("File system watcher stopped");
        await Task.CompletedTask;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("File created: {FullPath} at {Timestamp}", e.FullPath, DateTime.Now);
        ProcessFileEvent("Created", e.FullPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("File changed: {FullPath} at {Timestamp}", e.FullPath, DateTime.Now);
        ProcessFileEvent("Changed", e.FullPath);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("File deleted: {FullPath} at {Timestamp}", e.FullPath, DateTime.Now);
        ProcessFileEvent("Deleted", e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogInformation("File renamed: {OldFullPath} -> {FullPath} at {Timestamp}", 
            e.OldFullPath, e.FullPath, DateTime.Now);
        ProcessFileEvent("Renamed", e.FullPath, e.OldFullPath);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File system watcher error occurred");
    }

    private void ProcessFileEvent(string eventType, string filePath, string? oldPath = null)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var eventDetails = new
            {
                EventType = eventType,
                FilePath = filePath,
                OldPath = oldPath,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                Extension = fileInfo.Extension,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Processing file event: {EventDetails}", 
                System.Text.Json.JsonSerializer.Serialize(eventDetails, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Here you could implement business logic for different file types
            // For example: process images, validate documents, trigger workflows, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file event for: {FilePath}", filePath);
        }
    }

    private async Task CleanupOldTestFilesAsync()
    {
        try
        {
            if (!Directory.Exists(_watchDirectory)) return;

            var testFiles = Directory.GetFiles(_watchDirectory, "test_*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(3)
                .ToList();

            foreach (var file in testFiles)
            {
                File.Delete(file.FullName);
                _logger.LogDebug("Cleaned up old test file: {FileName}", file.Name);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old test files");
        }
    }

    public override void Dispose()
    {
        _fileWatcher?.Dispose();
        base.Dispose();
    }
}