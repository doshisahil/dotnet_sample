using Microsoft.Extensions.Logging;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service demonstrating resilience patterns concepts
/// </summary>
public interface IResilienceService
{
    Task<string> ExecuteWithRetryAsync(Func<Task<string>> operation);
    Task<string> ExecuteWithCircuitBreakerAsync(Func<Task<string>> operation);
    Task<string> ExecuteWithTimeoutAsync(Func<Task<string>> operation);
    Task DemonstrateResiliencePatternsAsync();
}

public class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly Dictionary<string, int> _circuitBreakerFailures = new();
    private readonly Dictionary<string, DateTime> _circuitBreakerOpenTime = new();

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteWithRetryAsync(Func<Task<string>> operation)
    {
        _logger.LogInformation("Executing operation with retry policy");
        
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                
                if (attempt == maxRetries)
                {
                    _logger.LogError("All retry attempts exhausted");
                    throw;
                }
                
                _logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
        
        throw new InvalidOperationException("This should never be reached");
    }

    public async Task<string> ExecuteWithCircuitBreakerAsync(Func<Task<string>> operation)
    {
        _logger.LogInformation("Executing operation with circuit breaker policy");
        
        const string circuitName = "default";
        const int failureThreshold = 3;
        var openDuration = TimeSpan.FromSeconds(10);
        
        // Check if circuit is open
        if (_circuitBreakerOpenTime.TryGetValue(circuitName, out var openTime))
        {
            if (DateTime.UtcNow - openTime < openDuration)
            {
                _logger.LogWarning("Circuit breaker is open, rejecting request");
                throw new InvalidOperationException("Circuit breaker is open");
            }
            else
            {
                _logger.LogInformation("Circuit breaker timeout expired, trying half-open state");
                _circuitBreakerOpenTime.Remove(circuitName);
                _circuitBreakerFailures[circuitName] = 0;
            }
        }
        
        try
        {
            var result = await operation();
            
            // Success - reset failure count
            _circuitBreakerFailures[circuitName] = 0;
            _logger.LogInformation("Circuit breaker: Operation succeeded, failures reset");
            
            return result;
        }
        catch (Exception ex)
        {
            // Increment failure count
            var failures = _circuitBreakerFailures.GetValueOrDefault(circuitName, 0) + 1;
            _circuitBreakerFailures[circuitName] = failures;
            
            _logger.LogWarning("Circuit breaker: Operation failed, failure count: {Failures}", failures);
            
            if (failures >= failureThreshold)
            {
                _circuitBreakerOpenTime[circuitName] = DateTime.UtcNow;
                _logger.LogError("Circuit breaker opened due to {Failures} consecutive failures", failures);
            }
            
            throw;
        }
    }

    public async Task<string> ExecuteWithTimeoutAsync(Func<Task<string>> operation)
    {
        _logger.LogInformation("Executing operation with timeout policy");
        
        var timeout = TimeSpan.FromSeconds(5);
        
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation();
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError("Operation timed out after {Timeout} seconds", timeout.TotalSeconds);
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }

    public async Task DemonstrateResiliencePatternsAsync()
    {
        _logger.LogInformation("Demonstrating resilience patterns");

        // Demo 1: Retry pattern with failing operation
        await DemoRetryPattern();

        // Demo 2: Circuit breaker pattern
        await DemoCircuitBreakerPattern();

        // Demo 3: Timeout pattern
        await DemoTimeoutPattern();
    }

    private async Task DemoRetryPattern()
    {
        _logger.LogInformation("=== Retry Pattern Demo ===");
        
        var attemptCount = 0;
        
        try
        {
            var result = await ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                _logger.LogInformation("Attempt {AttemptCount}", attemptCount);
                
                if (attemptCount < 3)
                {
                    throw new InvalidOperationException($"Simulated failure on attempt {attemptCount}");
                }
                
                return "Success after retries!";
            });
            
            _logger.LogInformation("Retry demo result: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retry demo failed");
        }
    }

    private async Task DemoCircuitBreakerPattern()
    {
        _logger.LogInformation("=== Circuit Breaker Pattern Demo ===");
        
        // Simulate multiple failures to open the circuit
        for (int i = 1; i <= 7; i++)
        {
            try
            {
                var result = await ExecuteWithCircuitBreakerAsync(async () =>
                {
                    if (i <= 4)
                    {
                        throw new InvalidOperationException($"Simulated failure {i}");
                    }
                    return $"Success on attempt {i}";
                });
                
                _logger.LogInformation("Circuit breaker demo result: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Circuit breaker demo attempt {Attempt} failed: {Message}", i, ex.Message);
            }
            
            await Task.Delay(100);
        }
    }

    private async Task DemoTimeoutPattern()
    {
        _logger.LogInformation("=== Timeout Pattern Demo ===");
        
        try
        {
            var result = await ExecuteWithTimeoutAsync(async () =>
            {
                // Simulate long-running operation
                await Task.Delay(6000); // This will timeout (limit is 5 seconds)
                return "This shouldn't complete due to timeout";
            });
            
            _logger.LogInformation("Timeout demo result: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Timeout demo failed as expected: {Message}", ex.Message);
        }
    }
}