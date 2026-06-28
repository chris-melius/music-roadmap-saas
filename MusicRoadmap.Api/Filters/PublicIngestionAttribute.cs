using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MusicRoadmap.Domain.Interfaces;
using System.Security.Claims;

namespace MusicRoadmap.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicIngestionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Resolve the validator using the request's DI Container
        var validator = context.HttpContext.RequestServices.GetRequiredService<IApiKeyValidator>();

        // 2. Extract the header token
        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var extractedKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key header 'X-API-KEY' is missing.");
            return;
        }

        // 3. Compare hashes using your validation logic
        var instructorId = await validator.GetInstructorIdFromKeyAsync(extractedKey.ToString());
        if (string.IsNullOrEmpty(instructorId))
        {
            context.Result = new UnauthorizedObjectResult("Invalid or inactive API Key.");
            return;
        }

        // 4. THE FIX: Assign a temporary, non-cookie identity to satisfy the global filter
        var claims = new[] { new Claim("InstructorId", instructorId) };
        var identity = new ClaimsIdentity(claims, "ApiKeyScheme");
        context.HttpContext.User = new ClaimsPrincipal(identity);
    }
}