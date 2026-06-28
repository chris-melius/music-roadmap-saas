namespace MusicRoadmap.Domain.Interfaces;

public interface ITokenService
{
    // Pass primitives or a Domain DTO, NOT ApplicationUser
    string CreateToken(string userId, string email, string instructorId);
    Task<string> GenerateAndSaveRefreshTokenAsync(string instructorId);
    string HashStringSha256(string rawToken);
}