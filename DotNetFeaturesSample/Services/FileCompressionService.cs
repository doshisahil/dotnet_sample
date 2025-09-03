using System.IO.Compression;
using Microsoft.Extensions.Logging;
using DotNetFeaturesSample.Models;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service demonstrating file compression and decompression (zip/unzip)
/// </summary>
public interface IFileCompressionService
{
    Task<FileOperationResult> CreateZipArchiveAsync(string sourceDirectory, string zipFilePath);
    Task<FileOperationResult> ExtractZipArchiveAsync(string zipFilePath, string extractPath);
    Task<FileOperationResult> CompressFileAsync(string filePath, string compressedFilePath);
    Task<FileOperationResult> DecompressFileAsync(string compressedFilePath, string outputFilePath);
    Task DemonstrateCompressionAsync();
}

public class FileCompressionService : IFileCompressionService
{
    private readonly ILogger<FileCompressionService> _logger;

    public FileCompressionService(ILogger<FileCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<FileOperationResult> CreateZipArchiveAsync(string sourceDirectory, string zipFilePath)
    {
        _logger.LogInformation("Creating zip archive from {SourceDirectory} to {ZipFilePath}", sourceDirectory, zipFilePath);
        
        try
        {
            if (!Directory.Exists(sourceDirectory))
            {
                Directory.CreateDirectory(sourceDirectory);
                // Create some sample files
                await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "sample1.txt"), "This is sample file 1");
                await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "sample2.txt"), "This is sample file 2");
            }

            ZipFile.CreateFromDirectory(sourceDirectory, zipFilePath);
            
            var result = new FileOperationResult
            {
                Success = true,
                Message = "Zip archive created successfully",
                FilePath = zipFilePath
            };

            _logger.LogInformation("Zip archive created successfully: {ZipFilePath}", zipFilePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create zip archive");
            return new FileOperationResult
            {
                Success = false,
                Message = $"Failed to create zip archive: {ex.Message}",
                FilePath = zipFilePath
            };
        }
    }

    public async Task<FileOperationResult> ExtractZipArchiveAsync(string zipFilePath, string extractPath)
    {
        _logger.LogInformation("Extracting zip archive from {ZipFilePath} to {ExtractPath}", zipFilePath, extractPath);
        
        try
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            
            var result = new FileOperationResult
            {
                Success = true,
                Message = "Zip archive extracted successfully",
                FilePath = extractPath
            };

            _logger.LogInformation("Zip archive extracted successfully to: {ExtractPath}", extractPath);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract zip archive");
            return new FileOperationResult
            {
                Success = false,
                Message = $"Failed to extract zip archive: {ex.Message}",
                FilePath = extractPath
            };
        }
    }

    public async Task<FileOperationResult> CompressFileAsync(string filePath, string compressedFilePath)
    {
        _logger.LogInformation("Compressing file {FilePath} to {CompressedFilePath}", filePath, compressedFilePath);
        
        try
        {
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, "Sample content for compression demonstration");
            }

            await using var originalFileStream = File.OpenRead(filePath);
            await using var compressedFileStream = File.Create(compressedFilePath);
            await using var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
            
            await originalFileStream.CopyToAsync(compressionStream);
            
            var originalSize = new FileInfo(filePath).Length;
            var compressedSize = new FileInfo(compressedFilePath).Length;
            var compressionRatio = (double)compressedSize / originalSize * 100;

            var result = new FileOperationResult
            {
                Success = true,
                Message = $"File compressed successfully. Original: {originalSize} bytes, Compressed: {compressedSize} bytes ({compressionRatio:F1}%)",
                FilePath = compressedFilePath
            };

            _logger.LogInformation("File compressed successfully: {Message}", result.Message);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress file");
            return new FileOperationResult
            {
                Success = false,
                Message = $"Failed to compress file: {ex.Message}",
                FilePath = compressedFilePath
            };
        }
    }

    public async Task<FileOperationResult> DecompressFileAsync(string compressedFilePath, string outputFilePath)
    {
        _logger.LogInformation("Decompressing file {CompressedFilePath} to {OutputFilePath}", compressedFilePath, outputFilePath);
        
        try
        {
            await using var compressedFileStream = File.OpenRead(compressedFilePath);
            await using var decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            await using var outputFileStream = File.Create(outputFilePath);
            
            await decompressionStream.CopyToAsync(outputFileStream);
            
            var result = new FileOperationResult
            {
                Success = true,
                Message = "File decompressed successfully",
                FilePath = outputFilePath
            };

            _logger.LogInformation("File decompressed successfully: {OutputFilePath}", outputFilePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decompress file");
            return new FileOperationResult
            {
                Success = false,
                Message = $"Failed to decompress file: {ex.Message}",
                FilePath = outputFilePath
            };
        }
    }

    public async Task DemonstrateCompressionAsync()
    {
        _logger.LogInformation("Demonstrating file compression and decompression");

        var tempDir = Path.Combine(Path.GetTempPath(), "DotNetSample", "compression");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a directory with sample files
            var sourceDir = Path.Combine(tempDir, "source");
            var zipFile = Path.Combine(tempDir, "archive.zip");
            var extractDir = Path.Combine(tempDir, "extracted");

            // Demonstrate zip operations
            var zipResult = await CreateZipArchiveAsync(sourceDir, zipFile);
            _logger.LogInformation("Zip creation result: {Result}", zipResult.Message);

            var extractResult = await ExtractZipArchiveAsync(zipFile, extractDir);
            _logger.LogInformation("Zip extraction result: {Result}", extractResult.Message);

            // Demonstrate file compression
            var sampleFile = Path.Combine(tempDir, "sample.txt");
            var compressedFile = Path.Combine(tempDir, "sample.gz");
            var decompressedFile = Path.Combine(tempDir, "sample_decompressed.txt");

            var compressResult = await CompressFileAsync(sampleFile, compressedFile);
            _logger.LogInformation("Compression result: {Result}", compressResult.Message);

            var decompressResult = await DecompressFileAsync(compressedFile, decompressedFile);
            _logger.LogInformation("Decompression result: {Result}", decompressResult.Message);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}