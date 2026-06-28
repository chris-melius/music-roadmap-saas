using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MusicRoadmap.Domain.Entities;
using MusicRoadmap.Infrastructure.Identity;

namespace MusicRoadmap.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public string CurrentInstructorId => _httpContextAccessor.HttpContext?.User
        .FindFirstValue("InstructorId") ?? string.Empty;        
        public AppDbContext(DbContextOptions<AppDbContext> options,
         IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Instructor> Instructors { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<AccountHolder> AccountHolders => Set<AccountHolder>();
        public DbSet<InstructorApiKey> InstructorApiKeys => Set<InstructorApiKey>();
        public DbSet<IngestionLog> IngestionLogs => Set<IngestionLog>();
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            
            // Optimize token lookup speeds
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Security Configuration for API Keys
        modelBuilder.Entity<InstructorApiKey>(entity =>
        {
            entity.HasIndex(e => e.HashedKey).IsUnique();
            entity.Property(e => e.InstructorId).IsRequired();
            
            // AUTOMATIC MULTI-TENANCY FILTER
            // Restricts read/update queries to the currently logged-in instructor
            entity.HasQueryFilter(k => k.InstructorId == CurrentInstructorId);
        });

        // Security Configuration for AccountHolders
        modelBuilder.Entity<AccountHolder>(entity =>
        {
            entity.Property(a => a.InstructorId).IsRequired();
            entity.Property(a => a.Status).HasDefaultValue("Pending");

            // Enforces database-level uniqueness for email per instructor
            entity.HasIndex(a => new { a.Email, a.InstructorId })
                .IsUnique();

            // MULTI-TENANCY PROTECTION
            entity.HasQueryFilter(a => a.InstructorId == CurrentInstructorId);
        });        

            // Force plural table names manually
            modelBuilder.Entity<Student>().ToTable("Students");
            modelBuilder.Entity<Instructor>().ToTable("Instructors");

            // Apply the filter: Every query for Students or Lessons will now 
            // automatically include "WHERE InstructorId = [CurrentUserId]"
            modelBuilder.Entity<Student>()
                .HasQueryFilter(s => s.InstructorId == CurrentInstructorId);

            modelBuilder.Entity<Lesson>()
                .HasQueryFilter(l => l.InstructorId == CurrentInstructorId);

            modelBuilder.Entity<AccountHolder>()
                .HasQueryFilter(a => a.InstructorId == CurrentInstructorId);

            modelBuilder.Entity<InstructorApiKey>()
                .HasQueryFilter(k => k.InstructorId == CurrentInstructorId);

            modelBuilder.Entity<Student>()
            .HasOne(s => s.Instructor)       // A Student has one Instructor
            .WithMany(i => i.Students)       // An Instructor has many Students
            .HasForeignKey(s => s.InstructorId) // Use THIS specific property as the key
            .IsRequired();

            // Establish 1 AccountHolder to Many Students relationship
            modelBuilder.Entity<Student>()
            .HasOne(s => s.AccountHolder)
                  .WithMany(a => a.Students)
                  .HasForeignKey(s => s.AccountHolderId)
                    .OnDelete(DeleteBehavior.Restrict); //  s.InstructorId == CurrentInstructorId);

            modelBuilder.Entity<Student>().Property(s=>s.InstructorId).IsRequired();

            modelBuilder.Entity<Lesson>()
                .Property(l => l.Revenue)
                .HasPrecision(18, 2); // Standard for currency in SQL Server

            modelBuilder.Entity<AccountHolder>()
                .HasOne(s => s.Instructor);       // An AccountHolder has one Instructor

            modelBuilder.Entity<AccountHolder>()
                .HasMany(s => s.Students);       // An AccountHolder can have one or many students

            // Configure relationships and constraints if needed
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Student)
                .WithMany(s => s.Lessons)
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.NoAction); // <--- THIS IS THE KEY LINE;
        }
    }
}