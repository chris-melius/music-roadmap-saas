using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Shared.DTOs;
using MusicRoadmap.Infrastructure.Data;

namespace MusicRoadmap.Api.Controllers;

[Authorize] // Locked down tightly via JWT Bearer tokens
[ApiController]
[Route("api/instructor/apikeys")]
public class InstructorApiKeysController(AppDbContext context, IApiKeyValidator validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ApiKeyResponseDto>>> GetMyKeys()
    {
        var keys = await context.InstructorApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyResponseDto
            {
                Id = k.Id,
                WebsiteURL = k.WebsiteURL,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            })
            .ToListAsync();

        return Ok(keys);
    }

    [HttpPost]
    public async Task<ActionResult<GeneratedKeyResultDto>> CreateKey([FromBody] CreateApiKeyDto dto)
    {
        var instructorId = User.FindFirstValue("InstructorId");
        if (string.IsNullOrEmpty(instructorId))
        {
            return Unauthorized("Instructor Identity claim is missing from JWT.");
        }

        // 2. Generate a secure crypto token (e.g., "mr_AbC123...")
        string rawToken = validator.GenerateRawKey();

        // 3. Hash it for secure database storage
        string hashedToken = validator.HashKey(rawToken);

        var keyRecord = new InstructorApiKey
        {
            InstructorId = instructorId,
            HashedKey = hashedToken,
            WebsiteURL = dto.WebsiteURL,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.InstructorApiKeys.Add(keyRecord);
        await context.SaveChangesAsync();

        // 4. Send the RAW token back ONLY ONCE to the UI
        var response = new GeneratedKeyResultDto
        {
            RawKey = rawToken,
            KeyDetails = new ApiKeyResponseDto
            {
                Id = keyRecord.Id,
                WebsiteURL = keyRecord.WebsiteURL,
                IsActive = keyRecord.IsActive,
                CreatedAt = keyRecord.CreatedAt
            }
        };

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeKey(Guid id)
    {
        // 1. Rely on the Global Query Filter to find the key
        // If an instructor tries to delete someone else's ID, this returns null (404)
        var keyRecord = await context.InstructorApiKeys.FindAsync(id);
        if (keyRecord == null)
        {
            return NotFound("Integration key not found or access denied.");
        }

        // 2. Soft-delete / instant kill the token visibility
        keyRecord.IsActive = false;
        await context.SaveChangesAsync();

        return NoContent();
    }
}