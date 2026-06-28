using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Shared.DTOs;
using MusicRoadmap.Infrastructure.Data;

namespace MusicRoadmap.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/instructor/review")]
public class AccountReviewController(AppDbContext context) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingRegistrationDto>>> GetPendingRegistrations()
    {
        // Fetch pending account holders and include their linked student details
        var pending = await context.AccountHolders
            .Where(a => a.Status == "Pending")
            .Include(a => a.Students)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PendingRegistrationDto
            {
                AccountHolderId = a.Id,
                ContactName = $"{a.FirstName} {a.LastName}",
                Email = a.Email,
                Phone = a.Phone,
                SubmittedAt = a.CreatedAt,
                Students = a.Students.Select(s => new PendingStudentDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    //Instrument = s.Instrument,
                    SkillLevel = s.SkillLevel
                }).ToList()
            })
            .ToListAsync();

        return Ok(pending);
    }

    [HttpPatch("approve/{id}")]
    public async Task<IActionResult> ApproveRegistration(string id)
    {
        var account = await context.AccountHolders.FindAsync(id);
        if (account == null)
        {
            return NotFound("Registration record not found or access denied.");
        }

        // Change status to active to make them show up in regular student lists
        account.Status = "Active";
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("reject/{id}")]
    public async Task<IActionResult> RejectRegistration(string id)
    {
        // Cascade rules we set up (Restrict) mean we should fetch and delete students manually
        var account = await context.AccountHolders
            .Include(a => a.Students)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
        {
            return NotFound("Registration record not found or access denied.");
        }

        // Wipe out the pending family unit entirely
        context.Students.RemoveRange(account.Students);
        context.AccountHolders.Remove(account);
        await context.SaveChangesAsync();

        return NoContent();
    }
}