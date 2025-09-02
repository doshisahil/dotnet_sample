using Microsoft.Extensions.Logging;

namespace DotNetFeaturesSample.Features;

/// <summary>
/// Feature demonstrating command line argument parsing concepts
/// </summary>
public class CommandLineFeature
{
    private readonly ILogger<CommandLineFeature> _logger;

    public CommandLineFeature(ILogger<CommandLineFeature> logger)
    {
        _logger = logger;
    }

    public async Task<int> ParseAndExecuteAsync(string[] args)
    {
        _logger.LogInformation("Demonstrating command line argument parsing");
        _logger.LogInformation("Received arguments: {Args}", string.Join(" ", args));

        // Simple argument parsing demonstration
        if (args.Length == 0)
        {
            await ShowHelpAsync();
            return 0;
        }

        var command = args[0].ToLower();
        
        switch (command)
        {
            case "demo":
                await HandleDemoCommandAsync(args);
                break;
            case "file":
                await HandleFileCommandAsync(args);
                break;
            case "server":
                await HandleServerCommandAsync(args);
                break;
            case "--help":
            case "-h":
                await ShowHelpAsync();
                break;
            default:
                _logger.LogWarning("Unknown command: {Command}", command);
                await ShowHelpAsync();
                return 1;
        }

        return 0;
    }

    private async Task ShowHelpAsync()
    {
        _logger.LogInformation("=== Command Line Parsing Help ===");
        _logger.LogInformation("Available commands:");
        _logger.LogInformation("  demo [feature] [--count N] [--delay N]  - Run feature demonstrations");
        _logger.LogInformation("  file compress <source> [--output path]  - Compress files");
        _logger.LogInformation("  file watch <directory> [--filter pattern] - Watch directory");
        _logger.LogInformation("  server [--port N] [--protocol type]     - Start server");
        _logger.LogInformation("  --help, -h                              - Show this help");
        _logger.LogInformation("");
        _logger.LogInformation("Examples:");
        _logger.LogInformation("  demo json --count 2");
        _logger.LogInformation("  file compress ./data --output data.zip");
        _logger.LogInformation("  server --port 8080 --protocol websocket");
        
        await Task.CompletedTask;
    }

    private async Task HandleDemoCommandAsync(string[] args)
    {
        var feature = args.Length > 1 ? args[1] : "all";
        var count = GetIntOption(args, "--count", 1);
        var delay = GetIntOption(args, "--delay", 1000);

        _logger.LogInformation("Running demo for feature: {Feature}", feature);
        _logger.LogInformation("Count: {Count}, Delay: {Delay}ms", count, delay);

        for (int i = 1; i <= count; i++)
        {
            _logger.LogInformation("Demo iteration {Iteration}/{Total}", i, count);
            
            await Task.Delay(delay);
            
            switch (feature.ToLower())
            {
                case "json":
                    _logger.LogInformation("JSON serialization demo would run here");
                    break;
                case "compression":
                    _logger.LogInformation("File compression demo would run here");
                    break;
                case "privacy":
                    _logger.LogInformation("Privacy compliance demo would run here");
                    break;
                case "resilience":
                    _logger.LogInformation("Resilience patterns demo would run here");
                    break;
                case "websocket":
                    _logger.LogInformation("WebSocket demo would run here");
                    break;
                case "all":
                default:
                    _logger.LogInformation("All feature demos would run here");
                    break;
            }
        }
    }

    private async Task HandleFileCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            _logger.LogWarning("File command requires subcommand (compress, watch)");
            return;
        }

        var subcommand = args[1].ToLower();
        
        switch (subcommand)
        {
            case "compress":
                var source = args.Length > 2 ? args[2] : "";
                var output = GetStringOption(args, "--output", "compressed.zip");
                _logger.LogInformation("Would compress {Source} to {Output}", source, output);
                break;
                
            case "watch":
                var directory = args.Length > 2 ? args[2] : "./";
                var filter = GetStringOption(args, "--filter", "*.*");
                _logger.LogInformation("Would watch directory {Directory} with filter {Filter}", directory, filter);
                break;
                
            default:
                _logger.LogWarning("Unknown file subcommand: {Subcommand}", subcommand);
                break;
        }
        
        await Task.CompletedTask;
    }

    private async Task HandleServerCommandAsync(string[] args)
    {
        var port = GetIntOption(args, "--port", 8080);
        var protocol = GetStringOption(args, "--protocol", "websocket");
        
        _logger.LogInformation("Would start {Protocol} server on port {Port}", protocol, port);
        
        await Task.CompletedTask;
    }

    private int GetIntOption(string[] args, string optionName, int defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == optionName && int.TryParse(args[i + 1], out int value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    private string GetStringOption(string[] args, string optionName, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == optionName)
            {
                return args[i + 1];
            }
        }
        return defaultValue;
    }

    public void DemonstrateCommandLineParsing()
    {
        _logger.LogInformation("=== Command Line Parsing Demo ===");
        _logger.LogInformation("System.CommandLine provides powerful command-line parsing capabilities:");
        _logger.LogInformation("- Hierarchical commands and subcommands");
        _logger.LogInformation("- Strongly typed options and arguments");
        _logger.LogInformation("- Automatic help generation");
        _logger.LogInformation("- Input validation and parsing");
        _logger.LogInformation("- Tab completion support");
        _logger.LogInformation("- Suggestion engines");
        _logger.LogInformation("");
        _logger.LogInformation("This demonstration uses simplified parsing logic.");
        _logger.LogInformation("In a real application, System.CommandLine would handle:");
        _logger.LogInformation("- Complex argument validation");
        _logger.LogInformation("- Type conversion");
        _logger.LogInformation("- Help text generation");
        _logger.LogInformation("- Error handling and reporting");
    }
}