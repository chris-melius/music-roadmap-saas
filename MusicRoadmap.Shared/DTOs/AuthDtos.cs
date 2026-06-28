namespace MusicRoadmap.Shared.DTOs;

// 1. Used to send valid token bundles back to the Client
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiration { get; set; }
}

// 2. Used by the Client to anonymously trade dead keys for fresh ones
public class RefreshTokenRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

// 3. Consolidated standard Login Contract
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// 4. Clean Instructor Registration Contract matching your exact properties
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class ErrorResponseDto
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Detailed { get; set; } = string.Empty; // Only populated during local dev
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}