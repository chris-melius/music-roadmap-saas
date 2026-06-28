using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicRoadmap.Domain.Entities;

public class UserRefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Links tightly back to ASP.NET Core Identity user strings
    public string UserId { get; set; } = string.Empty;
    
    // The cryptographically secure random token string
    [Required]
    public string Token { get; set; } = string.Empty;
    
    // Security properties to mitigate replay attacks
    [Required]
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    [Required]
    public string InstructorId { get; set; } = string.Empty;

    public Instructor? Instructor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
}