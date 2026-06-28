namespace MusicRoadmap.Shared.DTOs;

public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string WebsiteURL { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateApiKeyDto
{
    public string WebsiteURL { get; set; } = string.Empty;
}

public class GeneratedKeyResultDto
{
    public string RawKey { get; set; } = string.Empty;
    public ApiKeyResponseDto KeyDetails { get; set; } = new();
}