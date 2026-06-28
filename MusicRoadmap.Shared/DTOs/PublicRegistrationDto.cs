namespace MusicRoadmap.Shared.DTOs;
public class PublicRegistrationDto
{
    public string IngestionId { get; set; } = string.Empty;

    // 1. Account Holder / Contact Data (The Adult)
    public string ContactFirstName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    // 2. Student Context
    public bool IsAdultStudent { get; set; } // True if the contact IS the student
    
    // If IsAdultStudent is False, the form displays fields to collect the child's name
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;
    public string Instrument { get; set; } = "Piano";
    public string SkillLevel { get; set; } = string.Empty;
}