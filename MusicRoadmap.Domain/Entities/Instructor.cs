using System;
using System.Collections.Generic;

namespace MusicRoadmap.Domain.Entities
{
    public class Instructor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int AiCreditsRemaining { get; set; } = 5; // Give 5 free tokens on registration

        public string FullName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}