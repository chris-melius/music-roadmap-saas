using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;

namespace MusicRoadmap.Infrastructure.Services;

public class OpenAiRoadmapService(Kernel kernel) : IAiRoadmapService
{
    private readonly Kernel _kernel = kernel;

    public async Task<string> GenerateRoadmapAsync(Student student)
    {
        // 1. Configure the AI behavior (Structured JSON Output)
        var settings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = "json_object", // Forces valid JSON
            MaxTokens = 2000,
            Temperature = 1 // Balanced creativity for pedagogical suggestions
        };

        // 2. Craft the "Senior Instructor" Prompt
        var prompt = $@"
            You are a professional, world-class music instructor. 
            Create a highly personalized 12-week pedagogical roadmap for a student.

            STUDENT PROFILE:
            - Name: {student.FirstName}
            - Skill Level: {student.SkillLevel}
            - Primary Goals: {student.StudentGoals}
            - Weekly Practice Commitment: {student.MinutesPerWeekCommitment} minutes
            - Current Repertoire: {student.CurrentRepertoire ?? "None listed"}
            - Target Piece: {student.TargetPieces ?? "General improvement"}

            INSTRUCTIONS:
            - Break the plan into three 4-week phases.
            - Phase 1: Foundation. Phase 2: Development. Phase 3: Performance/Integration.
            - Suggest specific, real-world exercises (e.g., Hanon, Scales, Arpeggios) matching their skill level.
            - Ensure the difficulty of pieces is realistic for their level and practice time.

            OUTPUT FORMAT:
            Return ONLY a minified JSON object with this exact schema:
            {{
                ""studentName"": ""..."",
                ""minutesPerWeek"": 0,
                ""phase1"": {{ ""title"": ""..."", ""weeks"": ""Weeks 1-4"", ""exercises"": ""..."", ""pieces"": ""..."" }},
                ""phase2"": {{ ""title"": ""..."", ""weeks"": ""Weeks 5-8"", ""exercises"": ""..."", ""pieces"": ""..."" }},
                ""phase3"": {{ ""title"": ""..."", ""weeks"": ""Weeks 9-12"", ""exercises"": ""..."", ""pieces"": ""..."" }}
            }}";

        // 3. Dispatch to OpenAI
        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(settings));

        // 4. Return the raw JSON string
        return result.ToString();
    }
}