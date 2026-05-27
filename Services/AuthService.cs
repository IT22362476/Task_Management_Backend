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
            throw new ArgumentException("User already exists with this email");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RefreshToken = _tokenService.GenerateRefreshToken(),
            RefreshTokenExpiryTime = GetRefreshTokenExpiry()
        };

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
            throw new UnauthorizedAccessException("Invalid email or password");

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = GetRefreshTokenExpiry();
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
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = GetRefreshTokenExpiry();
        await _db.SaveChangesAsync();

        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }

    public async Task<TokenResponse> GoogleAuth(GoogleAuthRequest request)
    {
        var clientId = _config["GoogleAuth:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not configured");

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            request.IdToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            }
        );

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user == null)
        {
            user = new User
            {
                FullName = payload.Name ?? payload.Email,
                Email = payload.Email,
                Role = "Employee",
                Provider = "Google",
                IsActive = true
            };
            _db.Users.Add(user);
        }

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = GetRefreshTokenExpiry();
        await _db.SaveChangesAsync();

        return new TokenResponse(
            _tokenService.GenerateAccessToken(user),
            user.RefreshToken
        );
    }

    private DateTime GetRefreshTokenExpiry()
    {
        var days = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays");
        return DateTime.UtcNow.AddDays(days > 0 ? days : 7);
    }
}
