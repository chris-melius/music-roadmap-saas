using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Infrastructure.Data;
using MusicRoadmap.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace MusicRoadmap.Infrastructure.Identity;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService, AppDbContext context, IConfiguration config, ILogger<AuthController> logger) : ControllerBase
{

    [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    // ... Your existing password validation logic to find the instructor ...
    var instructor = await userManager.FindByEmailAsync(dto.Email);
    if (instructor == null || !await userManager.CheckPasswordAsync(instructor, dto.Password)) return Unauthorized(new { Message = "Invalid credentials." });

    // 1. Generate the stateless 15-minute Access Token
    var accessToken = tokenService.CreateToken(instructor.Id, instructor.Email, instructor.InstructorId);

    // 2. Generate, SHA-256 hash, and save the Refresh Token to the DB in one shot!
    var rawRefreshToken = await tokenService.GenerateAndSaveRefreshTokenAsync(instructor.InstructorId);

    // 3. Return the RAW plain-text tokens to the client
    return Ok(new AuthResponseDto
    {
        AccessToken = accessToken,
        RefreshToken = rawRefreshToken,
        AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15)
    });
}

    [HttpPost("register")]
    [EnableRateLimiting("RegistrationFormPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {

        // If the configuration is missing or blank, it falls back to a dummy string to prevent accidental bypasses.
        string allowedAdminIp = config["AdminSettings:AllowedIp"] ?? "BLOCKED_BY_DEFAULT"; 
        
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
        string remoteIpStr = string.Empty;

        if (remoteIpAddress != null)
        {
            remoteIpStr = remoteIpAddress.IsIPv4MappedToIPv6 
                ? remoteIpAddress.MapToIPv4().ToString() 
                : remoteIpAddress.ToString();
        }

        if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(rawIp, out var parsedIp))
            {
                remoteIpStr = parsedIp.IsIPv4MappedToIPv6 ? parsedIp.MapToIPv4().ToString() : parsedIp.ToString();
            }
        }

        // Enforce the fall-closed validation gate
        if (remoteIpStr != allowedAdminIp && remoteIpStr != "::1" && remoteIpStr != "127.0.0.1")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new 
            { 
                Message = "Registration is locked down to administrative IP addresses." 
            });
        }

        // 1. Create the Domain Instructor
        var instructor = new Instructor { FullName = $"{model.FirstName} {model.LastName}" };
        
        // 2. Setup the Security User
        var user = new ApplicationUser 
        { 
            UserName = model.Email, 
            Email = model.Email,
            InstructorId = instructor.Id, // Tie them together
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);

        // 3. Persist the Instructor business record
        context.Instructors.Add(instructor);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Registration successful" });
    }

private string GenerateSecureRefreshToken()
{
    // Generate an unguessable, cryptographically secure 64-byte random string
    var randomNumber = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
}

private async Task<AuthResponseDto> GenerateAuthTokensAsync(ApplicationUser user)
{
    // 1. PERFECT: Invoke your existing TokenService abstraction layer!
    // This cleanly handles the signing keys, audience parameters, and short-lived expiry arrays.
    string jwtAccessToken = tokenService.CreateToken(user.Id, user.Email!, user.InstructorId ?? string.Empty);

    // 2. Generate the Long-Lived Refresh Token
    string rawRefreshToken = GenerateSecureRefreshToken();

    var refreshTokenRecord = new UserRefreshToken
    {
        UserId = user.Id,
        Token = rawRefreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7), // Active for 7 days
        IsRevoked = false,
        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
    };

    // 3. Save to database (Bypasses global multi-tenant filters automatically if unregistered)
    context.UserRefreshTokens.Add(refreshTokenRecord);
    await context.SaveChangesAsync();

    return new AuthResponseDto
    {
        AccessToken = jwtAccessToken,
        RefreshToken = rawRefreshToken,
        AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15)
    };
}

[AllowAnonymous]
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
{
    if (string.IsNullOrEmpty(dto.RefreshToken)) return BadRequest();

    // 1. Hash the incoming raw token from the browser to match our database format
    string hashedIncomingToken = tokenService.HashStringSha256(dto.RefreshToken);

    // 2. Query against the hashed string value
    var storedToken = await context.UserRefreshTokens
        .FirstOrDefaultAsync(t => t.Token == hashedIncomingToken && !t.IsRevoked);

    if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
    {
        return Unauthorized(new { Message = "Session expired or invalid token." });
    }

    // 3. Rotate the token: Revoke the used one
    storedToken.IsRevoked = true;
    await context.SaveChangesAsync();

    // 4. Issue a pristine new pair
    var instructor = await context.Instructors.FindAsync(storedToken.InstructorId);

    var identityUser = await userManager.Users
        .FirstOrDefaultAsync(u => u.InstructorId == instructor.Id);
    
    if (identityUser == null || identityUser.Email == null)
    {
        return Unauthorized(new { Message = "Associated identity credentials could not be resolved." });
    }

    var newAccessToken = tokenService.CreateToken(instructor.Id, identityUser.Email, instructor.Id);
    var newRawRefreshToken = await tokenService.GenerateAndSaveRefreshTokenAsync(instructor.Id);

    return Ok(new AuthResponseDto
    {
        AccessToken = newAccessToken,
        RefreshToken = newRawRefreshToken,
        AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15)
    });
}

[HttpPost("logout")]
public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
{
    if (string.IsNullOrEmpty(dto.RefreshToken))
    {
        return BadRequest(new { Message = "Logout payload requires a valid refresh token." });
    }

    // 1. Convert the incoming plain-text client token into its SHA-256 hash
    string hashedIncomingToken = tokenService.HashStringSha256(dto.RefreshToken);

    // 2. Locate the active session row in your SQL Server database
    var storedToken = await context.UserRefreshTokens
        .FirstOrDefaultAsync(t => t.Token == hashedIncomingToken);

    if (storedToken != null)
    {
        // 3. HARD REVOCATION: Permanently kill the token in your database table!
        // This ensures the token can never be exploited or reused, even if stolen.
        storedToken.IsRevoked = true;
        await context.SaveChangesAsync();
    }

    return Ok(new { Message = "Logged out and session revoked successfully." });
}

}