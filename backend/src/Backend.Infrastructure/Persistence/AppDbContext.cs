using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LearningResource> LearningResources { get; set; }
    public DbSet<User> Users { get; set; }
    
    // Directory Block
    public DbSet<Language> Languages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Website> Websites { get; set; }
    public DbSet<LearningSection> LearningSections { get; set; }
    public DbSet<ListenVideo> ListenVideos { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<MockTest> MockTests { get; set; }
    public DbSet<TestSubmission> TestSubmissions { get; set; }
    public DbSet<Story> Stories { get; set; }
    
    // LMS Block
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Story
        modelBuilder.Entity<Story>(entity =>
        {
            entity.HasIndex(s => s.Slug).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();
            
            entity.HasOne(c => c.CreatedBy)
                .WithMany(u => u.CreatedCourses)
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Lesson
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasIndex(l => l.Slug).IsUnique();
            
            entity.HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Website
        modelBuilder.Entity<Website>(entity =>
        {
            entity.HasOne(w => w.Language)
                .WithMany(l => l.Websites)
                .HasForeignKey(w => w.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.Category)
                .WithMany(c => c.Websites)
                .HasForeignKey(w => w.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
