using System.ComponentModel.DataAnnotations.Schema;

namespace MusicRoadmap.Domain.Entities;

public class AccountHolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string InstructorId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Active, Suspended
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: One family account can have multiple students
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public Instructor Instructor {get; set;}= null!;

    // 2. TEMPORARY STUB: Satisfies old code expecting a Guid structure
    [NotMapped] // Prevents EF Core from writing this to SQL Server
    public Guid LegacyGuidId 
    {
        get => Guid.TryParse(Id, out var g) ? g : Guid.Empty;
        set => Id = value.ToString();
    }    
}