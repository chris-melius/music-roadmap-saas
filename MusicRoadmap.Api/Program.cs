using Microsoft.EntityFrameworkCore;
using MusicRoadmap.Infrastructure.Data;
using MusicRoadmap.Domain.Interfaces;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Infrastructure.Services;
using QuestPDF.Infrastructure;
using Microsoft.SemanticKernel;
using MusicRoadmap.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using MusicRoadmap.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using MusicRoadmap.Shared.DTOs;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MusicRoadmap.Api.Middleware;

// Required for QuestPDF community use
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
//  ENTERPRISE PRODUCTION SECURITY GUARD (FAIL-FAST)
// =========================================================================
if (builder.Environment.IsProduction() || builder.Environment.EnvironmentName == "Production")
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "";

    // Hard boundary rules checking for our fallback text strings
    bool isDbPlaceholder = connectionString.Contains("LOCAL_DEV_PLACEHOLDER") || string.IsNullOrEmpty(connectionString);
    bool isJwtPlaceholder = jwtKey.Contains("LOCAL_DEV_SECRET_KEY_PLACEHOLDER") || string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32;

    if (isDbPlaceholder || isJwtPlaceholder)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("=======================================================================");
        Console.Error.WriteLine("CRITICAL SECURITY FAULT: CONTAINER BOOT TERMINATED.");
        Console.Error.WriteLine("The hosting environment is marked as PRODUCTION, but the configuration");
        Console.Error.WriteLine("contains unsecure, hardcoded local development placeholder token strings.");
        Console.Error.WriteLine("=======================================================================");
        Console.ResetColor();

        // Hard exit to completely crash the container layout instantly
        Environment.Exit(1);
    }
}

// --- 1. Define the Policy ---
builder.Services.AddCors(options => { 
    options.AddPolicy("OpenCorsPolicy", policy => { 
        // Read the allowed production domains directly from your config settings!
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
                             ?? new string[] { "http://localhost:5001", "https://localhost:7001" }; // Local dev fallbacks

        policy.WithOrigins(allowedOrigins) 
              .AllowAnyHeader() 
              .AllowAnyMethod() 
              .AllowCredentials() 
              .WithExposedHeaders("Content-Disposition"); 
    }); 
});

// Configure ASP.NET Core to unpack and trust network proxy headers from Azure
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor 
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    
    // Clear restrictions so it trusts Azure Container App ingress nodes out of the box
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

//Register ai service

// Read a toggle from appsettings.json
bool useMockAi = builder.Configuration.GetValue<bool>("UseMockAi");

if (useMockAi)
{
    // Register the mock service (No OpenAI keys needed!)
    builder.Services.AddScoped<IAiRoadmapService, MockAiRoadmapService>();
}
else
{
    // Register the real OpenAI service we built earlier
    //Replace with OpenAiRoadmapService
    builder.Services.AddScoped<IAiRoadmapService, OpenAiRoadmapService>();
    
builder.Services.AddTransient<Kernel>(sp =>
{
    // HERE is where kernelBuilder is defined!
    // It is a helper object used only to configure the AI Kernel.
    var kernelBuilder = Kernel.CreateBuilder();
    string openAIKey = builder.Configuration["OpenAI:ApiKey"] ?? "";
    
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-5.5", // Use gpt-4o-mini for cost savings
        apiKey: openAIKey
    );

    return kernelBuilder.Build();
});
}

builder.Services.AddScoped<ITokenService, TokenService>();

// 1. MVC Options (where Filters/Security live)
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    //options.Filters.Add(new AuthorizeFilter(policy));
})
// 2. JSON Options (where Serialization/Cycles live)
.AddJsonOptions(options => 
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddAuthorization(options =>
{
});

// Register AppDbContext with the SQL Server provider
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the custom exception handler middleware dependencies
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // Generates standardized error metadata mapping

// Use your custom ApplicationUser so the system knows about InstructorId
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    // You can also tweak security settings here
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    // Force the API to use JWT, not Cookies, for every check
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // Prevents the automatic HTML redirect and sends 401 instead
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context => {
        return Task.CompletedTask;
    }

    };
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Essential for accessing user claims inside the DbContext
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();

builder.Services.AddRateLimiter(options =>
{
    // Token Bucket Policy
    options.AddTokenBucketLimiter("PublicFormPolicy", bucketOptions =>
    {
        bucketOptions.TokenLimit = 2; // Maximum capacity of the bucket (Allows up to 2 rapid submissions)
        bucketOptions.TokensPerPeriod = 1; // Refill just 1 token...
        bucketOptions.ReplenishmentPeriod = TimeSpan.FromHours(12); // ...every 12 hours!
        bucketOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        bucketOptions.QueueLimit = 0; // Drop burst overflows immediately
    });

    // THE ANTI-SPAM DEFENSE: A hyper-strict policy for the registration endpoint
    options.AddFixedWindowLimiter("RegistrationFormPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(5); // 5-minute security window
        opt.PermitLimit = 1;                  // Strictly 1 request allowed per IP
        opt.QueueLimit = 0;                   // Reject subsequent spammers instantly
    });

    // Allow an instructor to generate only 2 AI roadmaps per hour
    options.AddFixedWindowLimiter("AIEnginePromptPolicy", opt =>
    {
        opt.Window = TimeSpan.FromHours(1);
        opt.PermitLimit = 2;
        opt.QueueLimit = 0;
    });


    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

    // 1. Extract the endpoint metadata from the active HttpContext
    var endpoint = context.HttpContext.GetEndpoint();
    
    // 2. Safely grab the EnableRateLimitingAttribute metadata token if it exists
    var rateLimitAttribute = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
    
    // 3. Extract the active policy string name dynamically
    string? policyName = rateLimitAttribute?.PolicyName;

    // 4. Fallback to check if a policy was attached via Minimal APIs (RequireRateLimiting)
    if (string.IsNullOrEmpty(policyName))
    {
        // Internal fallback defaults if no string attribute token was matched
        policyName = "GlobalThrottlingPolicy";
    }

    // 5. Route the message string based on the policy context
    string feedbackMessage = policyName switch
    {
        "RegistrationFormPolicy" => "Registration limits exceeded. Please wait 5 minutes before trying again.",
        "AIEnginePromptPolicy"   => "AI roadmap processing cap reached. Please slow down your prompt generation requests.",
        _                        => "Too many concurrent requests detected. Operation throttled globally."
    };

    // 6. Stream the clean JSON payload back down the pipe
    await context.HttpContext.Response.WriteAsJsonAsync(new 
    { 
        Message = feedbackMessage,
        PolicyTriggered = policyName,
        Timestamp = DateTime.UtcNow
    }, token);
};
});

var app = builder.Build();

// Apply the forwarded headers middleware to trust the proxy headers from Azure
app.UseForwardedHeaders();

// Register the custom exception handler middleware
app.UseExceptionHandler(); 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); 
}

// Only enforce strict HTTPS routing if we are running outside local development loops!
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("OpenCorsPolicy"); 
app.UseRateLimiter();
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.MapPost("/api/public/registration/ingest", async (
    [FromHeader(Name = "X-API-KEY")] string apiKey, 
    [FromBody] PublicRegistrationDto dto, 
    AppDbContext context,
    IApiKeyValidator validator) =>
{
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Unauthorized();
    }

    using var transaction = await context.Database.BeginTransactionAsync();

   // 2. THE IDEMPOTENCY LOCK: Try to claim this request ID immediately
    try
    {
        var log = new IngestionLog { Id = dto.IngestionId };
        context.IngestionLogs.Add(log);
        await context.SaveChangesAsync(); // If this ID already exists, SQL Server throws an exception here!
    }
    catch (DbUpdateException)
    {
        // Duplicate request detected! Return a success message immediately so the client thinks it worked, 
        // preventing double records while maintaining a clean user experience.
        await Console.Error.WriteLineAsync($"[IDEMPOTENCY BLOCK] Prevented duplicate student write for Key: {dto.IngestionId}");
        return Results.Ok(new { message = "Registration successfully processed! Status: Pending Review." });
    }

    // 1. Re-enable the validator. This reads key, hashes it,
    // and returns the real 'InstructorId' we just configured in Step 2.
    var instructorId = await validator.GetInstructorIdFromKeyAsync(apiKey);
    if (string.IsNullOrEmpty(instructorId))
    {
        return Results.Json(new { error = "Invalid API Key." }, statusCode: 401);
    }

    // 2. THE SIBLING FIX: Look for an existing parent profile *only* under this instructor
    var accountHolder = await context.AccountHolders
        .IgnoreQueryFilters() // Bypass filter since the request has no active JWT login session
        .FirstOrDefaultAsync(a => a.Email.ToLower() == dto.ContactEmail.ToLower() 
                               && a.InstructorId == instructorId);

    if (accountHolder == null)
    {
        // Scenario A: Fresh family registration. Build a new parent profile from scratch.
        accountHolder = new AccountHolder
        {
            FirstName = dto.ContactFirstName,
            LastName = dto.ContactLastName,
            Email = dto.ContactEmail,
            Phone = dto.ContactPhone,
            InstructorId = instructorId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        context.AccountHolders.Add(accountHolder);
        await Console.Error.WriteLineAsync($"[DATA PIPELINE] Created brand new family unit for {dto.ContactEmail}");
    }
    else
    {
        // Scenario B: Sibling registration found! Link to existing profile and refresh phone number.
        accountHolder.Phone = dto.ContactPhone; // Keep contact data synced
        
        // If the parent was previously approved/active, we can choose to keep them active,
        // or set them back to 'Pending' so the instructor reviews the new sibling application.
        accountHolder.Status = "Pending"; 
        
        await Console.Error.WriteLineAsync($"[DATA PIPELINE] Sibling matching found for {dto.ContactEmail}. Appending child profile...");
    }
    
    // 3. Generate the Student profile mapping
    var student = new Student
    {
        InstructorId = instructorId,
        CreatedAt = DateTime.UtcNow,
        AccountHolder = accountHolder // EF Core handles foreign key connection matching on SaveChanges!
    };

    // 4. Resolve Self-Registration (Adult Student) vs Child-Registration
    if (dto.IsAdultStudent)
    {
        student.FirstName = dto.ContactFirstName;
        student.LastName = dto.ContactLastName;
        student.Notes = "Self-registered adult lesson applicant.";
    }
    else
    {
        student.FirstName = dto.StudentFirstName;
        student.LastName = dto.StudentLastName;
        student.Notes = $"Registered by parent/guardian: {dto.ContactFirstName} {dto.ContactLastName}";
    }
    context.Students.Add(student);

    await context.SaveChangesAsync();

    await transaction.CommitAsync();

    return Results.Ok(new { message = "Registration successfully processed! Status: Pending Review." });
})

.AllowAnonymous() // Keeps the global JWT/Cookie interceptors at bay
.RequireRateLimiting("PublicFormPolicy"); //Rate limiting

// =========================================================================
// RESILIENT AUTOMATED DATABASE INITIALIZATION LOOP
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    int retryCount = 0;
    int maxRetries = 6; // 6 attempts x 5 seconds = 30 seconds total patience window
    bool migrationSucceeded = false;

    while (!migrationSucceeded && retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to connect and migrate production database (Attempt {Count}/{Max})...", retryCount + 1, maxRetries);
            
            // Physical migration gate check
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Production database schema synchronized successfully with 0 errors.");
            migrationSucceeded = true;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning("Database engine is not fully initialized yet. Retrying in 5 seconds... Error: {Msg}", ex.Message);
            
            if (retryCount >= maxRetries)
            {
                logger.LogCritical(ex, "FATAL: Database connection timeout exceeded. Reached maximum retry boundaries.");
                if (!app.Environment.IsDevelopment())
                {
                    Environment.Exit(1); // Hard crash to notify the cloud hosting cluster
                }
            }
            else
            {
                // Sleep the execution thread for 5 seconds before checking again
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}

app.Run();

