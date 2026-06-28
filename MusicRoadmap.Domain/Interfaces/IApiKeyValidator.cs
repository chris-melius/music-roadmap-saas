namespace MusicRoadmap.Domain.Interfaces;

public interface IApiKeyValidator
{
    string GenerateRawKey();
    string HashKey(string rawKey);
    Task<string?> GetInstructorIdFromKeyAsync(string rawKey);
}