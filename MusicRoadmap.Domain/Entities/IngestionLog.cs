namespace MusicRoadmap.Domain.Entities;

public class IngestionLog
{
    public string Id { get; set; } = string.Empty; // Holds the DTO's IngestionId
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}