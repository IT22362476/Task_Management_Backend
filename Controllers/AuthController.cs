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
    public async Task<IActionResult> Register(RegisterRequest request)
        => Ok(await _auth.Register(request));

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth(GoogleAuthRequest request)
        => Ok(await _auth.GoogleAuth(request));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
        => Ok(await _auth.Login(request));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        => Ok(await _auth.RefreshToken(request));
}
