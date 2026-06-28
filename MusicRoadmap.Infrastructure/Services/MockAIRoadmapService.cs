using System;
using System.Threading.Tasks;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;

namespace MusicRoadmap.Infrastructure.Services;

public class MockAiRoadmapService : IAiRoadmapService
{
    public async Task<string> GenerateRoadmapAsync(Student student)
    {
        // 1. Simulate the AI "thinking" delay
        await Task.Delay(1500);

        // 2. Return a perfectly formatted, static mock JSON
        // This gives you concrete data to design your PDF layout with!
        string mockJson = $@"
        {{
            ""studentName"": ""{student.FirstName} {student.LastName}"",
            ""minutesPerWeek"": {student.MinutesPerWeekCommitment},
            ""phase1"": {{
                ""title"": ""Foundation & Hand Independence"",
                ""weeks"": ""Weeks 1-4"",
                ""exercises"": ""Hanon 1-5, C Major scale contrary motion."",
                ""pieces"": ""First 8 measures of {student.TargetPieces ?? "selected piece"} (hands separate).""
            }},
            ""phase2"": {{
                ""title"": ""Rhythmic Precision & Speed"",
                ""weeks"": ""Weeks 5-8"",
                ""exercises"": ""Metronome practice on syncopated patterns."",
                ""pieces"": ""Connecting hands together for {student.TargetPieces ?? "selected piece"}. Add dynamic contrast.""
            }},
            ""phase3"": {{
                ""title"": ""Performance Polish"",
                ""weeks"": ""Weeks 9-12"",
                ""exercises"": ""Arpeggio drills related to the piece's key signature."",
                ""pieces"": ""Full run-throughs of {student.TargetPieces ?? "selected piece"} targeting recital readiness.""
            }}
        }}";

        return mockJson;
    }
}