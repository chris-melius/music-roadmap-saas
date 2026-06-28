using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using MusicRoadmap.Shared.DTOs;

namespace MusicRoadmap.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. Log the absolute crash stack trace securely on your Docker server console terminal
        logger.LogError(exception, "[SERVER EXCEPTION CATCH] An unhandled error occurred: {Message}", exception.Message);

        // 2. Format the response headers
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // 3. Assemble the unified JSON envelope
        var errorResponse = new ErrorResponseDto
        {
            StatusCode = httpContext.Response.StatusCode,
            Message = "An internal server error occurred. The studio development team has been notified.",
            // Security Guard: Only expose raw stack traces while working locally in Development mode!
            Detailed = env.IsDevelopment() ? exception.StackTrace ?? string.Empty : "Protected Production Context.",
            Timestamp = DateTime.UtcNow
        };

        // 4. Stream the clean packet back down the network pipe to the client
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        // Returning true signals to ASP.NET Core that this exception has been caught 
        // and handled cleanly, preventing the underlying web server from breaking.
        return true;
    }
}