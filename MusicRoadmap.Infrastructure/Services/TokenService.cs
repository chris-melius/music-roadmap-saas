using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Domain.Entities; // Ensure your UserRefreshToken entity namespace is imported
using MusicRoadmap.Infrastructure.Data; // Ensure your AppDbContext namespace is imported
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MusicRoadmap.Infrastructure.Services;

public class TokenService(IConfiguration config, AppDbContext context) : ITokenService
{
    public string CreateToken(string userId, string email, string instructorId)
    {
        // 1. Define the claims (The "Identity Badge" contents)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new("InstructorId", instructorId),
            new(ClaimTypes.Role, "Teacher")
        };

        // 2. Setup the Security Key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        // 3. Describe the Token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // Token valid for 15 minutes
            SigningCredentials = creds,
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"]
        };

        // 4. Generate the actual Token string
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // 5. THE NEW PRODUCTION METHOD: Generates a raw token, hashes it, and saves it to the database
    public async Task<string> GenerateAndSaveRefreshTokenAsync(string instructorId)
    {
        // A. Generate an absolute cryptographically secure random string key sequence
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        string rawRefreshToken = Convert.ToBase64String(randomNumber);

        // B. Compute the immutable SHA-256 string hash for storage mapping
        string hashedDatabaseToken = HashStringSha256(rawRefreshToken);

        // C. Record the secure, obfuscated hash footprint inside your SQL database tables
        var refreshTokenEntity = new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            Token = hashedDatabaseToken, // ◄── Only the cryptographic hash is saved!
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        context.UserRefreshTokens.Add(refreshTokenEntity);
        await context.SaveChangesAsync();

        // D. Return the RAW plain-text token string to send to the browser's cookie storage
        return rawRefreshToken;
    }

    // 6. THE NEW VERIFICATION METHOD: Hashes incoming client token keys to compare against the database
    public string HashStringSha256(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        var builder = new StringBuilder();
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}