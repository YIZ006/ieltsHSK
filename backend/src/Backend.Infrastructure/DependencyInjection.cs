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
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
        services.AddScoped<IAuthService, Backend.Infrastructure.Services.AuthService>();
        services.AddScoped<Backend.Application.Abstractions.IR2StorageService, Backend.Infrastructure.Services.R2StorageService>();
        services.AddScoped<Backend.Application.Abstractions.IAiGradingService, Backend.Infrastructure.Services.AiGradingService>();
        services.AddScoped<Backend.Infrastructure.Services.YoutubeTranscriptService>();
        return services;
    }

    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await dbContext.Database.MigrateAsync();

        // Đảm bảo các bảng và ràng buộc khóa ngoại (Foreign Keys) luôn tồn tại trong PostgreSQL
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    -- 1. Bảng test_submissions: các cột và khóa ngoại tới users
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'test_submissions') THEN
                        ALTER TABLE test_submissions ADD COLUMN IF NOT EXISTS student_name TEXT;
                        ALTER TABLE test_submissions ADD COLUMN IF NOT EXISTS user_email TEXT;
                        ALTER TABLE test_submissions ADD COLUMN IF NOT EXISTS exam_title TEXT;
                        ALTER TABLE test_submissions ADD COLUMN IF NOT EXISTS attempt_number INTEGER NOT NULL DEFAULT 1;
                        ALTER TABLE test_submissions ADD COLUMN IF NOT EXISTS r2_storage_key TEXT;
                        
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'fk_test_submissions_users_user_id'
                        ) THEN
                            BEGIN
                                ALTER TABLE test_submissions 
                                ADD CONSTRAINT fk_test_submissions_users_user_id 
                                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;
                            EXCEPTION WHEN OTHERS THEN NULL;
                            END;
                        END IF;
                    END IF;

                    -- 2. Bảng listen_videos: cột user_id và khóa ngoại tới users
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'listen_videos') THEN
                        ALTER TABLE listen_videos ADD COLUMN IF NOT EXISTS user_id INTEGER;
                        
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'fk_listen_videos_users_user_id'
                        ) THEN
                            BEGIN
                                ALTER TABLE listen_videos 
                                ADD CONSTRAINT fk_listen_videos_users_user_id 
                                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;
                            EXCEPTION WHEN OTHERS THEN NULL;
                            END;
                        END IF;
                    END IF;

                    -- 3. Bảng ielts_vocabulary_progresses
                    CREATE TABLE IF NOT EXISTS ielts_vocabulary_progresses (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        vocabulary_id INTEGER NOT NULL REFERENCES ielts_vocabularies(id) ON DELETE CASCADE,
                        status TEXT NOT NULL DEFAULT 'Learned',
                        learned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        CONSTRAINT uq_ielts_vocab_progress_user_vocab UNIQUE (user_id, vocabulary_id)
                    );
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Column/FK migration note: {ex.Message}");
        }

        // Seed Admin User (cuongnane)
        if (!dbContext.Users.Any(u => u.Email == "cuong20067@gmail.com"))
        {
            var adminUser = new User
            {
                Username = "cuongnane",
                Email = "cuong20067@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Aa@cuongnane"),
                Role = "admin",
                Level = "C2",
                IsActive = true,
                LastLoginAt = DateTime.UtcNow
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }

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
            var admin = dbContext.Users.First(u => u.Email == "cuong20067@gmail.com");
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
    }
}
