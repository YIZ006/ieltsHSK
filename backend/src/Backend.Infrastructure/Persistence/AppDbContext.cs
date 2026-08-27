using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LearningResource> LearningResources { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }
    
    // Directory Block
    public DbSet<Language> Languages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Website> Websites { get; set; }
    public DbSet<LearningSection> LearningSections { get; set; }
    public DbSet<ListenVideo> ListenVideos { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<MockTest> MockTests { get; set; }
    public DbSet<HskMockTest> HskMockTests { get; set; }
    public DbSet<TestSubmission> TestSubmissions { get; set; }
    public DbSet<Story> Stories { get; set; }
    
    // LMS Block
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<HskVocabulary> HskVocabularies { get; set; }
    public DbSet<HskVocabularyImport> HskVocabularyImports { get; set; }
    public DbSet<HskVocabularyProgress> HskVocabularyProgresses { get; set; }
    public DbSet<IeltsVocabulary> IeltsVocabularies { get; set; }
    public DbSet<IeltsVocabularyImport> IeltsVocabularyImports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestSubmission>(entity =>
        {
            entity.Property(t => t.DetailsJson).HasColumnType("jsonb");
            entity.HasIndex(t => new { t.UserId, t.SubmittedAt });
        });

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

        // UserActivityLog
        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

        // HskVocabulary
        modelBuilder.Entity<HskVocabulary>(entity =>
        {
            entity.HasIndex(v => v.HskLevel);
            entity.HasIndex(v => new { v.HskLevel, v.Hanzi }).IsUnique();
        });

        // HskVocabularyProgress: mỗi user chỉ có 1 dòng tiến độ cho 1 từ
        modelBuilder.Entity<HskVocabularyProgress>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.VocabularyId }).IsUnique();
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // IeltsVocabulary: không cho trùng (từ + nghĩa) để tránh import lặp
        modelBuilder.Entity<IeltsVocabulary>(entity =>
        {
            entity.HasIndex(v => new { v.Word, v.Meaning }).IsUnique();
            entity.HasIndex(v => v.Topic);
        });
    }
}
