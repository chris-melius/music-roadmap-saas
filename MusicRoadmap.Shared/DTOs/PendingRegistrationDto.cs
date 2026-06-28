namespace MusicRoadmap.Shared.DTOs;

public class PendingRegistrationDto
{
    public string AccountHolderId { get; set; } = null!;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public List<PendingStudentDto> Students { get; set; } = new();
}

public class PendingStudentDto
{
    public string StudentId { get; set; } = null!;
    public string StudentName { get; set; } = string.Empty;
    public string Instrument { get; set; } = string.Empty;
    public string SkillLevel { get; set; } = string.Empty;
}