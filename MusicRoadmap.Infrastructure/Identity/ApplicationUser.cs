using Microsoft.AspNetCore.Identity;

namespace MusicRoadmap.Infrastructure.Identity;
public class ApplicationUser : IdentityUser
{
    // Custom properties
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
   // Link to your Domain model
    // This allows you to find the business 'Instructor' for this login
    public string? InstructorId { get; set; }
}