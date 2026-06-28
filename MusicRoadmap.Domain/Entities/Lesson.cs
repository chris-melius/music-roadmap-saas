using System;

namespace MusicRoadmap.Domain.Entities
{
    public class Lesson
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InstructorId { get; set; } = String.Empty;
        public string StudentId { get; set; } = String.Empty;
        public DateTime LessonDate { get; set; }
        public DateTime DurationMinutes { get; set; }
        public decimal Revenue { get; set; }
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Canceled
        public string Notes { get; set; } = string.Empty;

        // Navigation Properties
        public Instructor Instructor { get; set; } = null!;
        public Student Student { get; set; } = null!;
    }
}