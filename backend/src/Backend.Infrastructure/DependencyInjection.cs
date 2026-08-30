using Backend.Application.Abstractions;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
                {
                    // Supabase gói Free giới hạn số connection -> client-side pooling qua Port 6543
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention());
        services.AddScoped<IAuthService, Backend.Infrastructure.Services.AuthService>();
        services.AddScoped<Backend.Application.Abstractions.IR2StorageService, Backend.Infrastructure.Services.R2StorageService>();
        services.AddScoped<Backend.Infrastructure.Services.YoutubeTranscriptService>();
        return services;
    }

    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await dbContext.Database.MigrateAsync();

        // Clear existing user activity logs and all user data as requested
        var existingLogs = await dbContext.UserActivityLogs.ToListAsync();
        if (existingLogs.Count > 0) dbContext.UserActivityLogs.RemoveRange(existingLogs);

        var existingProgress = await dbContext.HskVocabularyProgresses.ToListAsync();
        if (existingProgress.Count > 0) dbContext.HskVocabularyProgresses.RemoveRange(existingProgress);

        var existingSubmissions = await dbContext.TestSubmissions.Where(s => s.UserId.HasValue).ToListAsync();
        foreach (var sub in existingSubmissions) sub.UserId = null;

        var existingUsers = await dbContext.Users.ToListAsync();
        if (existingUsers.Count > 0) dbContext.Users.RemoveRange(existingUsers);

        await dbContext.SaveChangesAsync();

        // Seed Languages
        if (!dbContext.Languages.Any(l => l.Code == "EN"))
        {
            dbContext.Languages.Add(new Language { Name = "Tiếng Anh", Code = "EN" });
        }
        if (!dbContext.Languages.Any(l => l.Code == "ZH"))
        {
            dbContext.Languages.Add(new Language { Name = "Tiếng Trung", Code = "ZH" });
        }
        await dbContext.SaveChangesAsync();

        // Seed Category
        if (!dbContext.Categories.Any(c => c.Name == "Luyện thi IELTS"))
        {
            dbContext.Categories.Add(new Category { Name = "Luyện thi IELTS", Description = "Tài nguyên luyện thi IELTS chuyên sâu" });
            dbContext.Categories.Add(new Category { Name = "Luyện nghe tiếng Anh", Description = "Websites luyện nghe chép chính tả" });
            await dbContext.SaveChangesAsync();
        }

        // Seed Websites
        if (!dbContext.Websites.Any(w => w.Title == "Cambridge Dictionary"))
        {
            var lang = dbContext.Languages.First(l => l.Code == "EN");
            var cat = dbContext.Categories.First(c => c.Name == "Luyện thi IELTS");
            
            dbContext.Websites.AddRange(
                new Website { 
                    Title = "Cambridge Dictionary", 
                    Url = "https://dictionary.cambridge.org/", 
                    Description = "Từ điển tiếng Anh uy tín nhất", 
                    LanguageId = lang.Id, 
                    CategoryId = cat.Id, 
                    IsFree = true,
                    RecommendedLevel = "Beginner to Advanced"
                },
                new Website { 
                    Title = "IELTS Simon", 
                    Url = "https://ielts-simon.com/", 
                    Description = "Kho tàng tài liệu IELTS từ cựu giám khảo Simon", 
                    LanguageId = lang.Id, 
                    CategoryId = cat.Id, 
                    IsFree = true,
                    RecommendedLevel = "IELTS 5.5+"
                }
            );
            await dbContext.SaveChangesAsync();
        }

        // Seed Course
        if (!dbContext.Courses.Any(c => c.Slug == "ielts-listening-master"))
        {
            var admin = dbContext.Users.FirstOrDefault();
            if (admin != null)
            {
                dbContext.Courses.Add(new Course
                {
                    Title = "IELTS Listening Masterclass",
                    Slug = "ielts-listening-master",
                    Description = "Khóa học luyện nghe IELTS chuyên sâu từ con số 0.",
                    Level = "B2",
                    Category = "listening",
                    Status = "published",
                    DurationMinutes = 1200,
                    CreatedById = admin.Id
                });
                await dbContext.SaveChangesAsync();
            }
        }

        // Seed LearningSections
        if (!dbContext.LearningSections.Any())
        {
            dbContext.LearningSections.AddRange(
                new LearningSection { Name = "Luyện đề", Description = "Chưa thử — bắt đầu góc này?", Icon = "bi-journal-text", Route = "/ielts/luyen-de", Language = "IELTS", OrderIndex = 1 },
                new LearningSection { Name = "Nghe & điền từ", Description = "Chưa thử — bắt đầu góc này?", Icon = "bi-headphones", Route = "/ielts/nghe-dien", Language = "IELTS", OrderIndex = 2 },
                new LearningSection { Name = "Nói theo", Description = "Chưa thử — bắt đầu góc này?", Icon = "bi-mic", Route = "/ielts/noi-theo", Language = "IELTS", OrderIndex = 3 },
                new LearningSection { Name = "Đọc truyện", Description = "222 truyện theo trình độ đang chờ", Icon = "bi-book", Route = "/ielts/doc-truyen", Language = "IELTS", OrderIndex = 4 },
                new LearningSection { Name = "Từ vựng", Description = "Kho 15.000 từ A1->C2", Icon = "bi-spellcheck", Route = "/ielts/tu-vung", Language = "IELTS", OrderIndex = 5 },
                new LearningSection { Name = "Ưu tiên ôn tập", Description = "Chưa có dữ liệu — làm để ra lỗ hổng", Icon = "bi-bar-chart-steps", Route = "/ielts/uu-tien", Language = "IELTS", OrderIndex = 6 }
            );
            await dbContext.SaveChangesAsync();
        }

        // Seed HSK LearningSections
        if (!dbContext.LearningSections.Any(s => s.Language == "HSK"))
        {
            dbContext.LearningSections.AddRange(
                new LearningSection { Name = "Luyện đề HSK", Description = "Luyện đề thi HSK các cấp", Icon = "bi-journal-text", Route = "/hsk/luyen-de", Language = "HSK", OrderIndex = 1 },
                new LearningSection { Name = "Nghe HSK", Description = "Luyện nghe HSK", Icon = "bi-headphones", Route = "/hsk/listening", Language = "HSK", OrderIndex = 2 },
                new LearningSection { Name = "Đọc HSK", Description = "Luyện đọc HSK", Icon = "bi-book", Route = "/hsk/reading", Language = "HSK", OrderIndex = 3 },
                new LearningSection { Name = "Viết HSK", Description = "Luyện viết HSK", Icon = "bi-pencil", Route = "/hsk/writing", Language = "HSK", OrderIndex = 4 },
                new LearningSection { Name = "Nói HSK", Description = "Luyện nói HSK", Icon = "bi-mic", Route = "/hsk/speaking", Language = "HSK", OrderIndex = 5 },
                new LearningSection { Name = "Từ vựng HSK", Description = "Từ vựng HSK 1-9", Icon = "bi-spellcheck", Route = "/hsk/tu-vung", Language = "HSK", OrderIndex = 6 }
            );
            await dbContext.SaveChangesAsync();
        }

        // Seed TOEIC LearningSections
        if (!dbContext.LearningSections.Any(s => s.Language == "TOEIC"))
        {
            dbContext.LearningSections.AddRange(
                new LearningSection { Name = "Dashboard", Description = "Tổng quan TOEIC", Icon = "bi-speedometer2", Route = "/toeic", Language = "TOEIC", OrderIndex = 1 },
                new LearningSection { Name = "Luyện đề", Description = "Đề thi chuẩn ETS", Icon = "bi-journal-text", Route = "/toeic/test", Language = "TOEIC", OrderIndex = 2 },
                new LearningSection { Name = "Từ vựng", Description = "Flashcard 70 từ", Icon = "bi-layers", Route = "/toeic/flashcards", Language = "TOEIC", OrderIndex = 3 },
                new LearningSection { Name = "Nghe Part 1-4", Description = "Luyện Listening", Icon = "bi-headphones", Route = "/toeic/listening", Language = "TOEIC", OrderIndex = 4 },
                new LearningSection { Name = "Đọc Part 5-7", Description = "Luyện Reading", Icon = "bi-book", Route = "/toeic/reading", Language = "TOEIC", OrderIndex = 5 }
            );
            await dbContext.SaveChangesAsync();
        }
    }
}
