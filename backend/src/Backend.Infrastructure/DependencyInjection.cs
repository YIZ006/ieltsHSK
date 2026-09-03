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
        var connStr = configuration.GetConnectionString("DefaultConnection") 
                      ?? configuration["ConnectionStrings:DefaultConnection"]
                      ?? configuration["ConnectionStrings__DefaultConnection"];

        if (!string.IsNullOrWhiteSpace(connStr))
        {
            connStr = connStr.Trim().Trim('"').Trim('\'');
            if (connStr.StartsWith("ConnectionStrings__DefaultConnection=", StringComparison.OrdinalIgnoreCase))
                connStr = connStr.Substring("ConnectionStrings__DefaultConnection=".Length).Trim();
            else if (connStr.StartsWith("ConnectionStrings:DefaultConnection=", StringComparison.OrdinalIgnoreCase))
                connStr = connStr.Substring("ConnectionStrings:DefaultConnection=".Length).Trim();
            else if (connStr.StartsWith("DefaultConnection=", StringComparison.OrdinalIgnoreCase))
                connStr = connStr.Substring("DefaultConnection=".Length).Trim();

            // Strip SQL Server-only parameters like Trust Server Certificate which causes Npgsql to throw
            connStr = System.Text.RegularExpressions.Regex.Replace(
                connStr, 
                @";?\s*Trust\s*Server\s*Certificate\s*=\s*(true|false);?", 
                ";", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim(';', ' ');
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connStr, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Redis Distributed Cache & In-Memory Fallback
        var redisConn = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var redisEnabled = configuration.GetValue<bool?>("Redis:Enabled") ?? true;

        if (redisEnabled && !string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                var options = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                options.AbortOnConnectFail = false; // Resilience: Do not crash if Redis is unavailable on startup
                options.ConnectTimeout = 3000;
                options.SyncTimeout = 3000;
                options.AsyncTimeout = 3000;
                return StackExchange.Redis.ConnectionMultiplexer.Connect(options);
            });
        }
        else
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ => null!);
        }

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, Backend.Infrastructure.Services.RedisCacheService>();

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
        
        // 1. Đồng bộ __EFMigrationsHistory với các bảng/cột đã tồn tại trong PostgreSQL
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    -- Tạo bảng __EFMigrationsHistory nếu chưa tồn tại
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        migration_id character varying(150) NOT NULL,
                        product_version character varying(32) NOT NULL,
                        CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
                    );

                    -- 1. InitialPostgreSQL & InitialCreate_PostgreSQL
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'categories') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260826062942_InitialPostgreSQL', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;

                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260826102953_InitialCreate_PostgreSQL', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;

                    -- 2. AddFullNameToUser
                    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'full_name') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260826164733_AddFullNameToUser', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;

                    -- 3. AddHskMockTestEntity
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'hsk_mock_tests') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260827111448_AddHskMockTestEntity', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;

                    -- 4. AddIeltsVocabularyImport
                    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'test_submissions' AND column_name = 'audio_key') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260828100248_AddIeltsVocabularyImport', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;

                    -- 5. AddSubmissionDetailsAndR2Key
                    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'test_submissions' AND column_name = 'r2_storage_key') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260828111500_AddSubmissionDetailsAndR2Key', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;

                    -- 6. AddCefrLevelToVocabulary
                    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'ielts_vocabularies' AND column_name = 'cefr_level') 
                       AND EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ielts_vocabulary_progresses') THEN
                        INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version)
                        VALUES ('20260829160132_AddCefrLevelToVocabulary', '9.0.4')
                        ON CONFLICT (migration_id) DO NOTHING;
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Pre-migration history sync note: {ex.Message}");
        }

        // 2. Chạy MigrateAsync
        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] MigrateAsync note: {ex.Message}");
        }

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
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ielts_vocabularies') THEN
                        CREATE TABLE IF NOT EXISTS ielts_vocabulary_progresses (
                            id SERIAL PRIMARY KEY,
                            user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                            vocabulary_id INTEGER NOT NULL REFERENCES ielts_vocabularies(id) ON DELETE CASCADE,
                            status TEXT NOT NULL DEFAULT 'Learned',
                            learned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                            CONSTRAINT uq_ielts_vocab_progress_user_vocab UNIQUE (user_id, vocabulary_id)
                        );
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Column/FK migration note: {ex.Message}");
        }

        try
        {
            // Seed Admin User (cuongnane)
            if (!await dbContext.Users.AnyAsync(u => u.Email == "cuong20067@gmail.com"))
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

            // Ensure Admin role for admins
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE users SET role = 'admin' WHERE email IN ('cuong20067@gmail.com', 'phamc13579@gmail.com');
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Admin role update note: {ex.Message}");
            }

            // Seed Languages
            if (!await dbContext.Languages.AnyAsync(l => l.Code == "EN"))
            {
                dbContext.Languages.Add(new Language { Name = "Tiếng Anh", Code = "EN" });
            }
            if (!await dbContext.Languages.AnyAsync(l => l.Code == "ZH"))
            {
                dbContext.Languages.Add(new Language { Name = "Tiếng Trung", Code = "ZH" });
            }
            await dbContext.SaveChangesAsync();

            // Seed Category
            if (!await dbContext.Categories.AnyAsync(c => c.Name == "Luyện thi IELTS"))
            {
                dbContext.Categories.Add(new Category { Name = "Luyện thi IELTS", Description = "Tài nguyên luyện thi IELTS chuyên sâu" });
                dbContext.Categories.Add(new Category { Name = "Luyện nghe tiếng Anh", Description = "Websites luyện nghe chép chính tả" });
                await dbContext.SaveChangesAsync();
            }

            // Seed Websites
            if (!await dbContext.Websites.AnyAsync(w => w.Title == "Cambridge Dictionary"))
            {
                var lang = await dbContext.Languages.FirstOrDefaultAsync(l => l.Code == "EN");
                var cat = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Luyện thi IELTS");
                
                if (lang != null && cat != null)
                {
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
            }

            // Seed Course
            if (!await dbContext.Courses.AnyAsync(c => c.Slug == "ielts-listening-master"))
            {
                var admin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "cuong20067@gmail.com");
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
            if (!await dbContext.LearningSections.AnyAsync())
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
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "HSK"))
            {
                dbContext.LearningSections.AddRange(
                    new LearningSection { Name = "Luyện đề HSK", Description = "Luyện đề thi HSK các cấp", Icon = "bi-journal-text", Route = "/hsk/luyen-de", Language = "HSK", OrderIndex = 1 },
                    new LearningSection { Name = "Từ vựng HSK", Description = "Từ vựng HSK 1-9", Icon = "bi-spellcheck", Route = "/hsk/tu-vung", Language = "HSK", OrderIndex = 2 }
                );
                await dbContext.SaveChangesAsync();
            }

            // Seed TOEIC LearningSections
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "TOEIC"))
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
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Data seeding note: {ex.Message}");
        }

        // Seed GrammarStructures table & sample data
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""GrammarStructures"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""StructureCode"" text NOT NULL,
                    ""BandLevel"" text NOT NULL,
                    ""Category"" text NOT NULL,
                    ""GrammarTopic"" text NOT NULL,
                    ""Formula"" text NOT NULL,
                    ""UsageFunction"" text NOT NULL,
                    ""BasicExample"" text,
                    ""AdvancedExample"" text NOT NULL,
                    ""VietnameseMeaning"" text NOT NULL,
                    ""KeyCollocations"" text,
                    ""CommonMistakes"" text,
                    ""PracticeExercise"" text,
                    ""Tags"" text,
                    ""DisplayOrder"" integer NOT NULL DEFAULT 0,
                    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" timestamp with time zone
                );
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] GrammarStructures table creation note: {ex.Message}");
        }

        if (!dbContext.GrammarStructures.Any())
        {
            dbContext.GrammarStructures.AddRange(
                new GrammarStructure
                {
                    StructureCode = "W_INV_01",
                    BandLevel = "7.5 - 8.5",
                    Category = "Writing Task 2",
                    GrammarTopic = "Đảo ngữ (Inversion)",
                    Formula = "Not only + Aux + S + V, but S + (also) + V",
                    UsageFunction = "Nhấn mạnh 2 tác động song hành, tạo ấn tượng học thuật mạnh ở mở đoạn hoặc câu chủ đề",
                    BasicExample = "Computers help students study and they make work easier.",
                    AdvancedExample = "Not only does technological adoption facilitate self-directed learning, but it also enhances workforce productivity.",
                    VietnameseMeaning = "Không chỉ việc áp dụng công nghệ tạo điều kiện cho việc tự học, mà nó còn nâng cao năng suất của lực lượng lao động.",
                    KeyCollocations = "technological adoption, facilitate self-directed learning, workforce productivity",
                    CommonMistakes = "Quên đảo trợ động từ lên trước chủ ngữ sau 'Not only' (ví dụ viết sai: Not only computers help...)",
                    PracticeExercise = "Rewrite: Tourism creates jobs and it also introduces local culture.",
                    Tags = "inversion, emphasis, task2, academic",
                    DisplayOrder = 1
                },
                new GrammarStructure
                {
                    StructureCode = "W_NOM_01",
                    BandLevel = "7.0 - 8.0",
                    Category = "Writing Task 1 & 2",
                    GrammarTopic = "Danh từ hóa (Nominalisation)",
                    Formula = "The [Noun phrase] + led to / resulted in + a [Adj] [Noun]",
                    UsageFunction = "Biến đổi câu văn nói chứa động từ thành văn phong học thuật khách quan, trang trọng",
                    BasicExample = "People used more renewable energy so emissions decreased rapidly.",
                    AdvancedExample = "The widespread adoption of renewable energy resulted in a substantial reduction in carbon emissions.",
                    VietnameseMeaning = "Việc áp dụng rộng rãi năng lượng tái tạo đã dẫn đến sự sụt giảm đáng kể lượng phát thải carbon.",
                    KeyCollocations = "widespread adoption, substantial reduction, carbon emissions",
                    CommonMistakes = "Dùng sai giới từ đi kèm với danh từ (ví dụ: reduction of thay vì reduction in)",
                    PracticeExercise = "Rewrite: Cars increased rapidly so air became heavily polluted.",
                    Tags = "nominalisation, academic_style, task1, task2",
                    DisplayOrder = 2
                },
                new GrammarStructure
                {
                    StructureCode = "W_CLEFT_01",
                    BandLevel = "7.5 - 8.5",
                    Category = "Writing Task 2",
                    GrammarTopic = "Câu chẻ (Cleft Sentence)",
                    Formula = "It is/was + [Thành phần nhấn mạnh] + that/who + [Mệnh đề]",
                    UsageFunction = "Nhấn mạnh chính xác chủ thể chịu trách nhiệm hoặc giải pháp cốt lõi cho một vấn đề",
                    BasicExample = "The government should solve this problem, not citizens.",
                    AdvancedExample = "It is the municipal authorities, rather than individuals, that must take decisive action against urban pollution.",
                    VietnameseMeaning = "Chính các cơ quan chính quyền đô thị, chứ không phải các cá nhân, mới là bên phải hành động quyết liệt để chống lại ô nhiễm.",
                    KeyCollocations = "municipal authorities, take decisive action, urban pollution",
                    CommonMistakes = "Dùng nhầm 'which' thay vì 'that' khi thành phần nhấn mạnh là danh từ chỉ vật",
                    PracticeExercise = "Rewrite: Early education shapes a child's future, not higher education.",
                    Tags = "cleft_sentence, emphasis, solutions, task2",
                    DisplayOrder = 3
                },
                new GrammarStructure
                {
                    StructureCode = "W_PART_01",
                    BandLevel = "7.0 - 8.0",
                    Category = "Writing Task 1",
                    GrammarTopic = "Mệnh đề phân từ (Participle Clause)",
                    Formula = "[Main Clause], thereby + V-ing / leading to + [Noun phrase]",
                    UsageFunction = "Diễn tả chuỗi biến động kết quả liên hoàn trong bài mô tả biểu đồ Task 1",
                    BasicExample = "The car sales rose to 50,000 and this made it the most popular product.",
                    AdvancedExample = "Car sales surged to 50,000 units in 2020, thereby overtaking motorbikes as the leading vehicle category.",
                    VietnameseMeaning = "Doanh số ô tô tăng vọt lên 50.000 chiếc vào năm 2020, qua đó vượt qua xe máy để trở thành nhóm phương tiện dẫn đầu.",
                    KeyCollocations = "surge to, overtake, leading vehicle category",
                    CommonMistakes = "Dùng 'thereby + V nguyên mẫu' thay vì 'thereby + V-ing'",
                    PracticeExercise = "Rewrite: Company revenue doubled in Q3 and this allowed further expansion.",
                    Tags = "participle, task1, trend, cause_effect",
                    DisplayOrder = 4
                }
            );
            await dbContext.SaveChangesAsync();
        }
    }
}

