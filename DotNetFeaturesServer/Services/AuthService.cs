using Microsoft.EntityFrameworkCore;
using DotNetFeaturesServer.Data;
using DotNetFeaturesServer.Models;

namespace DotNetFeaturesServer.Services;

/// <summary>
/// Service for user authentication and management
/// </summary>
public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(string username, string email, string password);
    Task UpdateLastLoginAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        _logger.LogInformation("Authenticating user {Username}", username);
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            
        if (user == null)
        {
            _logger.LogWarning("User {Username} not found or inactive", username);
            return null;
        }
        
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user {Username}", username);
            return null;
        }
        
        _logger.LogInformation("User {Username} authenticated successfully", username);
        return user;
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }
    
    public async Task<User> CreateUserAsync(string username, string email, string password)
    {
        _logger.LogInformation("Creating new user {Username}", username);
        
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this username or email already exists");
        }
        
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {Username} created successfully with ID {UserId}", username, user.Id);
        return user;
    }
    
    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}