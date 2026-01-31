namespace Task_Manager_Backend.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role
);

public record LoginRequest(
    string Email,
    string Password
);

public record TokenResponse(
    string AccessToken,
    string RefreshToken
);

public record RefreshTokenRequest(
    string RefreshToken
);
