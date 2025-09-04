using Microsoft.AspNetCore.Mvc;
using DotNetFeaturesServer.Services;
using System.ComponentModel.DataAnnotations;

namespace DotNetFeaturesServer.Controllers;

/// <summary>
/// Controller for authentication and user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        ITelemetryService telemetryService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _telemetryService = telemetryService;
        _logger = logger;
    }
    
    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (!ModelState.IsValid)
            {
                await LogApiCall(0, "login", "POST", 400, startTime);
                return BadRequest(ModelState);
            }
            
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);
            if (user == null)
            {
                await LogApiCall(0, "login", "POST", 401, startTime);
                return Unauthorized(new { message = "Invalid username or password" });
            }
            
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            
            await _authService.UpdateLastLoginAsync(user.Id);
            
            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                },
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            
            await LogApiCall(user.Id, "login", "POST", 200, startTime);
            _telemetryService.IncrementCounter("user_login", new Dictionary<string, object?> { ["user_id"] = user.Id });
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            await LogApiCall(0, "login", "POST", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (!ModelState.IsValid)
            {
                await LogApiCall(0, "register", "POST", 400, startTime);
                return BadRequest(ModelState);
            }
            
            var user = await _authService.CreateUserAsync(request.Username, request.Email, request.Password);
            
            var response = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };
            
            await LogApiCall(user.Id, "register", "POST", 201, startTime);
            _telemetryService.IncrementCounter("user_registration", new Dictionary<string, object?> { ["user_id"] = user.Id });
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for username {Username}", request.Username);
            await LogApiCall(0, "register", "POST", 409, startTime);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username {Username}", request.Username);
            await LogApiCall(0, "register", "POST", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Get user information by ID
    /// </summary>
    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                await LogApiCall(0, $"user/{id}", "GET", 404, startTime);
                return NotFound(new { message = "User not found" });
            }
            
            var response = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };
            
            await LogApiCall(user.Id, $"user/{id}", "GET", 200, startTime);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            await LogApiCall(0, $"user/{id}", "GET", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Validate JWT token
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                await LogApiCall(0, "validate", "POST", 400, startTime);
                return BadRequest(new { message = "Token is required" });
            }
            
            var principal = _jwtService.ValidateToken(request.Token);
            if (principal == null)
            {
                await LogApiCall(0, "validate", "POST", 401, startTime);
                return Unauthorized(new { message = "Invalid token" });
            }
            
            var userIdClaim = principal.FindFirst("user_id")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                await LogApiCall(userId, "validate", "POST", 200, startTime);
                return Ok(new { valid = true, userId = userId });
            }
            
            await LogApiCall(0, "validate", "POST", 401, startTime);
            return Unauthorized(new { message = "Invalid token claims" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            await LogApiCall(0, "validate", "POST", 500, startTime);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    private async Task LogApiCall(int userId, string endpoint, string method, int statusCode, DateTime startTime)
    {
        var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        
        await _telemetryService.LogApiCallAsync(userId, $"api/auth/{endpoint}", method, statusCode, duration, 
            "", "", userAgent, ipAddress);
    }
}

// Request/Response models
public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class ValidateTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}