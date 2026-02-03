using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.Helpers;
using Task_Manager_Backend.Models;
using Google.Apis.Auth;
namespace Task_Manager_Backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, TokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    public async Task<TokenResponse> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("User already exists");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)
        );

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }

    public async Task<TokenResponse> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)
        );

        await _db.SaveChangesAsync();

        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }

    public async Task<TokenResponse> RefreshToken(RefreshTokenRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.RefreshToken == request.RefreshToken &&
                 u.RefreshTokenExpiryTime > DateTime.UtcNow
        );

        if (user == null)
            throw new Exception("Invalid refresh token");

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)
        );

        await _db.SaveChangesAsync();

        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }

    public async Task<TokenResponse> GoogleAuth(GoogleAuthRequest request)
    {
        // 1. Validate Google ID token
        var payload = await GoogleJsonWebSignature.ValidateAsync(
            request.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[]
                {
                _config["GoogleAuth:ClientId"]
                ?? throw new Exception("Google ClientId not configured")
                }
            }
        );

        // 2. Check if user already exists
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        // 3. If first-time Google login → create user
        if (user == null)
        {
            user = new User
            {
                FullName = payload.Name ?? payload.Email,
                Email = payload.Email,
                Role = "Employee",              // default role
                Provider = "Google",
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // 4. Generate refresh token
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)
        );

        await _db.SaveChangesAsync();

        // 5. Return tokens
        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }
}
