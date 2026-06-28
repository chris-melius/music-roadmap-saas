using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace MusicRoadmap.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoadmapsController : ControllerBase
{

    private readonly IAiRoadmapService _aiService;
    private readonly AppDbContext _context;
    
    public RoadmapsController(IAiRoadmapService aiService, AppDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    //Post:api/roadmaps/generate/5
    [HttpGet("generate/{studentId}")]
    [EnableRateLimiting("AIEnginePromptPolicy")]
    public async Task<ActionResult<string>> GenerateRoadmap(string studentId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var student = await _context.Students.FindAsync(studentId);

        var instructor = await _context.Instructors.FindAsync(User.FindFirstValue("InstructorId"));

        if (instructor == null)
        {
            return Unauthorized();
        }

        if (instructor.AiCreditsRemaining <= 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                Message = "You have exhausted your monthly AI roadmap tokens. Please upgrade your subscription tier."
            });
        }

        if (student == null)
        {
            return NotFound($"Student with ID {studentId} not found.");
        }

        instructor.AiCreditsRemaining--;
       
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        string roadmapJson = await _aiService.GenerateRoadmapAsync(student);

        // 1. Generate the PDF into a byte array
        byte[] pdfBytes = Reports.RoadmapDocument.GeneratePdfBytes(roadmapJson);

        // 2. Return the file stream directly to the browser
        return File(pdfBytes, "application/pdf", $"{student.LastName}_Roadmap.pdf");
    }
}