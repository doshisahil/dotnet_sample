using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotNetFeaturesSample.Models;

namespace DotNetFeaturesSample.Services;

/// <summary>
/// Service demonstrating WebSocket support
/// </summary>
public interface IWebSocketService
{
    Task StartServerAsync(int port, CancellationToken cancellationToken = default);
    Task StopServerAsync();
    Task SendMessageToAllClientsAsync(string message);
    bool IsRunning { get; }
}

public class WebSocketService : BackgroundService, IWebSocketService
{
    private readonly ILogger<WebSocketService> _logger;
    private readonly SampleSettings _settings;
    private HttpListener? _httpListener;
    private readonly List<WebSocket> _connectedClients = new();
    private readonly object _clientsLock = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public WebSocketService(
        ILogger<WebSocketService> logger,
        IOptions<SampleSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public bool IsRunning => _httpListener?.IsListening == true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.Features.EnableWebSocket)
        {
            await StartServerAsync(_settings.Features.WebSocketPort, stoppingToken);
        }
    }

    public async Task StartServerAsync(int port, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting WebSocket server on port {Port}", port);

        try
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{port}/");
            _httpListener.Start();

            _logger.LogInformation("WebSocket server started successfully on http://localhost:{Port}/", port);

            // Start accepting connections
            _ = Task.Run(async () => await AcceptConnectionsAsync(_cancellationTokenSource.Token));

            // Start sending periodic messages
            _ = Task.Run(async () => await SendPeriodicMessagesAsync(_cancellationTokenSource.Token));

            // Simulate client connection for demo
            _ = Task.Run(async () => await SimulateClientConnectionAsync(port, _cancellationTokenSource.Token));

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket server error");
            throw;
        }
    }

    public async Task StopServerAsync()
    {
        _logger.LogInformation("Stopping WebSocket server");

        _cancellationTokenSource?.Cancel();
        
        // Close all connected clients
        lock (_clientsLock)
        {
            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket client");
                }
            }
            _connectedClients.Clear();
        }

        _httpListener?.Stop();
        _httpListener?.Close();
        _httpListener = null;

        _logger.LogInformation("WebSocket server stopped");
        await Task.CompletedTask;
    }

    public async Task SendMessageToAllClientsAsync(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var clientsToRemove = new List<WebSocket>();

        lock (_clientsLock)
        {
            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        clientsToRemove.Add(client);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sending message to WebSocket client");
                    clientsToRemove.Add(client);
                }
            }

            // Remove disconnected clients
            foreach (var client in clientsToRemove)
            {
                _connectedClients.Remove(client);
            }
        }

        if (clientsToRemove.Any())
        {
            _logger.LogInformation("Removed {Count} disconnected clients", clientsToRemove.Count);
        }

        await Task.CompletedTask;
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener != null)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(async () => await ProcessWebSocketAsync(context, cancellationToken));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // WSA_OPERATION_ABORTED
            {
                // Expected when listener is stopped
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting WebSocket connection");
            }
        }
    }

    private async Task ProcessWebSocketAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        WebSocket webSocket = null;
        try
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            webSocket = webSocketContext.WebSocket;

            lock (_clientsLock)
            {
                _connectedClients.Add(webSocket);
            }

            _logger.LogInformation("WebSocket client connected. Total clients: {Count}", _connectedClients.Count);

            // Send welcome message
            var welcomeMessage = "Welcome to .NET Core WebSocket server!";
            var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(welcomeBytes), WebSocketMessageType.Text, true, cancellationToken);

            // Handle messages from client
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Received message from client: {Message}", message);
                    
                    // Echo the message back
                    var response = $"Echo: {message} (received at {DateTime.Now:HH:mm:ss})";
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection error");
        }
        finally
        {
            if (webSocket != null)
            {
                lock (_clientsLock)
                {
                    _connectedClients.Remove(webSocket);
                }
                _logger.LogInformation("WebSocket client disconnected. Total clients: {Count}", _connectedClients.Count);
            }
        }
    }

    private async Task SendPeriodicMessagesAsync(CancellationToken cancellationToken)
    {
        var messageCount = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, cancellationToken); // Send every 10 seconds
                
                messageCount++;
                var periodicMessage = $"Periodic server message #{messageCount} at {DateTime.Now:HH:mm:ss}";
                await SendMessageToAllClientsAsync(periodicMessage);
                
                _logger.LogInformation("Sent periodic message to {Count} clients", _connectedClients.Count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending periodic message");
            }
        }
    }

    private async Task SimulateClientConnectionAsync(int port, CancellationToken cancellationToken)
    {
        // Wait a bit for server to start
        await Task.Delay(2000, cancellationToken);
        
        try
        {
            using var client = new ClientWebSocket();
            var uri = new Uri($"ws://localhost:{port}/");
            
            _logger.LogInformation("Simulating client connection to {Uri}", uri);
            await client.ConnectAsync(uri, cancellationToken);
            
            // Send a test message
            var testMessage = "Hello from simulated client!";
            var messageBytes = Encoding.UTF8.GetBytes(testMessage);
            await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
            
            // Receive messages for a short time
            var buffer = new byte[1024 * 4];
            var endTime = DateTime.Now.AddSeconds(15);
            
            while (client.State == WebSocketState.Open && DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), 
                        new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation("Client received: {Message}", message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout receiving message, continue
                }
            }
            
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Demo complete", cancellationToken);
            _logger.LogInformation("Simulated client disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in simulated client connection");
        }
    }

    public override void Dispose()
    {
        StopServerAsync().GetAwaiter().GetResult();
        _cancellationTokenSource?.Dispose();
        base.Dispose();
    }
}