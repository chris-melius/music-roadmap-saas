using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Infrastructure.Data;

namespace MusicRoadmap.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StudentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents()
    {
        // For now, we return all students. 
        // Later we'll filter by the logged-in InstructorId.
        return await _context.Students
        .Where(s => s.AccountHolder == null || s.AccountHolder.Status == "Active")
        .Select(s => new StudentDto
        {
            Id = s.Id,
            FirstName = s.FirstName,
            LastName = s.LastName,
            SkillLevel = s.SkillLevel,
            StudentGoals = s.StudentGoals,
            TargetPieces = s.TargetPieces,
            MinutesPerWeekCommitment = s.MinutesPerWeekCommitment
        })
        .ToListAsync();
    }

    // POST: api/students
    [HttpPost]
    public async Task<ActionResult<Student>> CreateStudent(StudentDto dto)
    {
        var instructorId = User.FindFirstValue("InstructorId");
        var student = new Student
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            SkillLevel = dto.SkillLevel,
            LastLessonDate = dto.LastLessonDate,
            JoinDate = dto.JoinDate,
            CreatedAt = dto.CreatedAt,
            CurrentRepertoire = dto.CurrentRepertoire,
            TargetPieces = dto.TargetPieces,
            StudentGoals = dto.StudentGoals,
            MinutesPerWeekCommitment = dto.MinutesPerWeekCommitment,
            InstructorId = instructorId!
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStudents), new { id = student.Id }, student);
    }
}
