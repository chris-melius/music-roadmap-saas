using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Infrastructure.Data;

namespace MusicRoadmap.Infrastructure.Security;

public class ApiKeyValidator(AppDbContext context) : IApiKeyValidator
{
    private const string KeyPrefix = "mr_"; // MusicRoadmap indicator prefix

    public string GenerateRawKey()
    {
        // 1. Generate a cryptographically strong 32-byte random token
        var randomBytes = new byte[32];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(randomBytes);

        // 2. Convert to string and append a professional system prefix
        var secureToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", ""); // Strip non-url friendly characters

        return $"{KeyPrefix}{secureToken}";
    }

    public string HashKey(string rawKey)
    {
        // 3. Hash the key before hitting the DB using SHA256
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<string?> GetInstructorIdFromKeyAsync(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey)) return null;

        var incomingHash = HashKey(rawKey);

        // 4. IMPORTANT: Use IgnoreQueryFilters() here!
        // When an external website calls this, they don't have a JWT token yet.
        // If we don't bypass the filter, 'CurrentInstructorId' is blank, and the lookup fails.
        var keyRecord = await context.InstructorApiKeys
            .IgnoreQueryFilters() 
            .FirstOrDefaultAsync(k => k.HashedKey == incomingHash && k.IsActive);

        return keyRecord?.InstructorId;
    }
}