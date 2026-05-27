using Microsoft.AspNetCore.Mvc;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.Services;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.Register(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.Login(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Login successful"));
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        var result = await _auth.GoogleAuth(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Google authentication successful"));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _auth.RefreshToken(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, "Token refreshed successfully"));
    }
}
