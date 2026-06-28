using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicRoadmap.Domain.Entities
{
    public class StudentDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InstructorId { get; set; } = String.Empty;
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]        
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters.")]        
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Skill Level is required.")]
        [StringLength(50, ErrorMessage = "Skill Level cannot exceed 50 characters.")]        

        public string SkillLevel { get; set; } = string.Empty;

        public DateTime? LastLessonDate { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CurrentRepertoire { get; set; }

        public string? TargetPieces { get; set; }
        public string? StudentGoals { get; set; }

        public int? MinutesPerWeekCommitment { get; set; }
    }
}