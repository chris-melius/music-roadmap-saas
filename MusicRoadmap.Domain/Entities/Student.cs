using System;
using System.Collections.Generic;

namespace MusicRoadmap.Domain.Entities
{
    public class Student
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InstructorId { get; set; } = String.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string SkillLevel { get; set; } = string.Empty;

        public DateTime? LastLessonDate { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CurrentRepertoire { get; set; }

        public string? TargetPieces { get; set; }
        public string? StudentGoals { get; set; }

        public int? MinutesPerWeekCommitment { get; set; }

        public string? Notes { get; set; }

        public string? AccountHolderId { get; set; }
            
        // Navigation Properties
        public AccountHolder? AccountHolder { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

        public Instructor Instructor {get; set; } = null!;
    }
}