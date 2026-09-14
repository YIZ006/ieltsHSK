using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
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
    public DbSet<IeltsVocabularyProgress> IeltsVocabularyProgresses { get; set; }
    public DbSet<ToeicVocabulary> ToeicVocabularies { get; set; }
    public DbSet<ToeicVocabularyProgress> ToeicVocabularyProgresses { get; set; }
    public DbSet<UserStudyActivity> UserStudyActivities { get; set; }
    public DbSet<UserGameProgress> UserGameProgresses { get; set; }
    public DbSet<ExamCheckpoint> ExamCheckpoints { get; set; }
    public DbSet<GrammarStructure> GrammarStructures { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserNotificationRead> UserNotificationReads { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Friendship
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");
            entity.HasIndex(f => new { f.UserId, f.FriendId }).IsUnique();
            entity.HasIndex(f => f.UserId);
            entity.HasIndex(f => f.FriendId);
            entity.HasIndex(f => f.Status);

            entity.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Friend)
                .WithMany()
                .HasForeignKey(f => f.FriendId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("admins");
            entity.HasIndex(a => a.Email).IsUnique();
            entity.HasIndex(a => a.Username).IsUnique();
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

        // RefreshToken: tra cứu nhanh theo giá trị token, xoá Cascade khi user bị xoá
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasIndex(t => new { t.UserId, t.ExpiresAt });
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
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
                .WithMany(u => u.HskVocabularyProgresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ListenVideo
        modelBuilder.Entity<ListenVideo>(entity =>
        {
            entity.HasOne(v => v.User)
                .WithMany(u => u.ListenVideos)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // IeltsVocabulary: không cho trùng (từ + nghĩa) để tránh import lặp
        modelBuilder.Entity<IeltsVocabulary>(entity =>
        {
            entity.HasIndex(v => new { v.Word, v.Meaning }).IsUnique();
            entity.HasIndex(v => v.Topic);
        });

        // IeltsVocabularyProgress: mỗi user chỉ có 1 dòng tiến độ cho 1 từ IELTS
        modelBuilder.Entity<IeltsVocabularyProgress>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.VocabularyId }).IsUnique();
            entity.HasOne(p => p.User)
                .WithMany(u => u.IeltsVocabularyProgresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Vocabulary)
                .WithMany()
                .HasForeignKey(p => p.VocabularyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // JSONB: tiến trình học tập có cấu trúc động (bài nghe lưu đáp án,
        // bài đọc lưu chi tiết câu hỏi...) -> dùng kiểu jsonb của PostgreSQL
        modelBuilder.Entity<TestSubmission>(entity =>
        {
            entity.Property(s => s.DetailsJson).HasColumnType("jsonb");
            entity.Property(s => s.R2StorageKey).HasColumnName("r2_storage_key");
            entity.HasIndex(s => new { s.UserId, s.Skill });
            entity.HasIndex(s => new { s.UserId, s.SubmittedAt });
            entity.HasOne(s => s.User)
                .WithMany(u => u.TestSubmissions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // JSONB cho nội dung truyện (paragraphs / vocabulary / questions)
        modelBuilder.Entity<Story>(entity =>
        {
            entity.HasIndex(s => s.Slug).IsUnique();
            entity.Property(s => s.ContentJson).HasColumnType("jsonb");
            entity.Property(s => s.VocabularyJson).HasColumnType("jsonb");
            entity.Property(s => s.QuestionsJson).HasColumnType("jsonb");
        });

        // UserStudyActivity
        modelBuilder.Entity<UserStudyActivity>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.ActivityDate }).IsUnique();
            entity.HasOne(a => a.User)
                .WithMany(u => u.StudyActivities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ToeicVocabulary
        modelBuilder.Entity<ToeicVocabulary>(entity =>
        {
            entity.HasIndex(v => v.Topic);
            entity.HasIndex(v => new { v.Word, v.Meaning });
            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ToeicVocabularyProgress
        modelBuilder.Entity<ToeicVocabularyProgress>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.VocabularyId }).IsUnique();
            entity.HasOne(p => p.User)
                .WithMany(u => u.ToeicVocabularyProgresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Vocabulary)
                .WithMany(v => v.Progresses)
                .HasForeignKey(p => p.VocabularyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserGameProgress
        modelBuilder.Entity<UserGameProgress>(entity =>
        {
            entity.HasIndex(g => new { g.UserId, g.GameType, g.Level }).IsUnique();
            entity.HasOne(g => g.User)
                .WithMany(u => u.GameProgresses)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamCheckpoint
        modelBuilder.Entity<ExamCheckpoint>(entity =>
        {
            entity.Property(c => c.CheckpointDataJson).HasColumnType("jsonb");
            entity.HasIndex(c => new { c.UserIdentifier, c.Skill, c.ExamUrl });
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserNotificationRead
        modelBuilder.Entity<UserNotificationRead>(entity =>
        {
            entity.ToTable("user_notification_reads");
            entity.HasIndex(r => new { r.UserId, r.NotificationId }).IsUnique();
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Notification)
                .WithMany()
                .HasForeignKey(r => r.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
