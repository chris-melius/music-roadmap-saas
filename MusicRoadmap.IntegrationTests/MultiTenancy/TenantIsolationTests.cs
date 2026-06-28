using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Infrastructure.Data;
using MusicRoadmap.Shared.DTOs;
using Xunit;

namespace MusicRoadmap.IntegrationTests.MultiTenancy;

// 1. A custom mock authentication handler to bypass cryptography signature checks in tests
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check which instructor tenant context header our test client injected
        Context.Request.Headers.TryGetValue("X-Test-InstructorId", out var instructorId);
        
        if (string.IsNullOrEmpty(instructorId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test instructor ID header."));
        }

        // Build mock JWT claims mapping properties matching your real security scheme
        var claims = new[] { 
            new Claim(ClaimTypes.NameIdentifier, instructorId.ToString()),
            new Claim("InstructorId", instructorId.ToString()) // Multi-tenant anchor claim
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuthScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TenantIsolationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TenantIsolationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Evict the production SQL Server registry
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Inject isolated In-Memory DB container
                services.AddDbContext<AppDbContext>((container, options) =>
                {
                    options.UseInMemoryDatabase("TenantTestDbSandbox")
                           .UseInternalServiceProvider(new ServiceCollection()
                               .AddEntityFrameworkInMemoryDatabase()
                               .BuildServiceProvider());
                });

                // 2. THE SECURITY BYPASS: Force the API to use our mock authentication engine in testing
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestAuthScheme";
                    options.DefaultChallengeScheme = "TestAuthScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuthScheme", options => { });
            });
        });
    }

    [Fact]
    public async Task GetStudents_ShouldEnforceAbsoluteTenantIsolation_AndNeverLeakCrossTenantData()
    {
        // ARRANGE: Establish two separate tenant keys
        var instructorIdA = "instructor_alpha_guid_111";
        var instructorIdB = "instructor_beta_guid_222";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            // Insert a student that belongs EXCLUSIVELY to Instructor A (Tenant A)
            var privateStudent = new Student
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "Tommy",
                LastName = "Sibling",
                SkillLevel = "Beginner",
                InstructorId = instructorIdA // Locked to Tenant A
            };

            db.Students.Add(privateStudent);
            await db.SaveChangesAsync();
        }

        // ACT: Simulate an active request from Instructor B (The Attacker / Tenant B)
        var client = _factory.CreateClient();
        
        // Pass Instructor B's tenant context via our custom test header
        client.DefaultRequestHeaders.Add("X-Test-InstructorId", instructorIdB);

        var response = await client.GetAsync("api/students");

        // ASSERT: Verify your Global Query Filter successfully blocked the leak
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var studentsReturned = await response.Content.ReadFromJsonAsync<List<StudentDto>>();
        Assert.NotNull(studentsReturned);
        
        // THE ULTIMATE PROOF: The test passes ONLY if Instructor B receives 0 students, 
        // proving Tommy's record remained completely hidden inside Tenant A's database sandbox boundary!
        Assert.Empty(studentsReturned);
    }
}