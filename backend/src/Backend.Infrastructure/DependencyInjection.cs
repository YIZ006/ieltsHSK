using Backend.Application.Abstractions;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

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

            // Tối ưu hóa cho kết nối Supabase Pooler (PgBouncer/Supavisor):
            // - KeepAlive=30: Tránh AWS NAT tự ngắt kết nối socket khi rảnh, loại bỏ tình trạng đơ/chậm sau vài phút
            // - No Reset On Close=true: Tránh lỗi và độ trễ do lệnh DISCARD ALL trên pooler
            // - Minimum Pool Size=1: Mở sẵn kết nối ấm, triệt tiêu độ trễ TLS handshake ở request đầu tiên
            // - Connection Idle Lifetime=60: Tự động dọn kết nối cũ an toàn
            try
            {
                var csb = new NpgsqlConnectionStringBuilder(connStr);
                if (csb.Host != null && csb.Host.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (csb.KeepAlive <= 0) csb.KeepAlive = 30;
                    if (!connStr.Contains("No Reset On Close", StringComparison.OrdinalIgnoreCase))
                        csb["No Reset On Close"] = true;
                    if (csb.MinPoolSize <= 0) csb.MinPoolSize = 1;
                    if (csb.ConnectionIdleLifetime <= 0) csb.ConnectionIdleLifetime = 60;
                    connStr = csb.ConnectionString;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup Warning] NpgsqlConnectionStringBuilder: {ex.Message}");
            }
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
            var lazyMultiplexer = new Lazy<StackExchange.Redis.IConnectionMultiplexer>(() =>
            {
                try
                {
                    var options = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                    options.AbortOnConnectFail = false; // Resilience: Do not crash if Redis is unavailable
                    options.ConnectTimeout = 1000;      // Fast timeout: không làm nghẽn request đầu nếu Redis tắt
                    options.SyncTimeout = 1000;
                    options.AsyncTimeout = 1000;
                    return StackExchange.Redis.ConnectionMultiplexer.Connect(options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Redis Notice] Could not connect to Redis ({redisConn}): {ex.Message}. Falling back to MemoryCache.");
                    return null!;
                }
            });

            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ => lazyMultiplexer.Value);
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

                -- Tạo bảng admins riêng biệt không chung với users
                CREATE TABLE IF NOT EXISTS admins (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(100) NOT NULL UNIQUE,
                    full_name VARCHAR(255) NOT NULL,
                    email VARCHAR(255) NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    role VARCHAR(50) NOT NULL DEFAULT 'admin',
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    last_login_at TIMESTAMP WITH TIME ZONE NULL,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE NULL
                );

                -- Khóa chặt: Toàn bộ bảng users chỉ là học viên (role = 'user'), tuyệt đối không có quyền admin
                UPDATE users SET role = 'user' WHERE role != 'user';

                -- Tự động sửa sequence cho learning_sections nếu bị lệch ID
                DO $seq$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'learning_sections') THEN
                        PERFORM setval(pg_get_serial_sequence('learning_sections', 'id'), COALESCE(MAX(id), 1)) FROM learning_sections;
                    END IF;
                END $seq$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Column/FK migration note: {ex.Message}");
        }

        try
        {
            // 1. Luôn đảm bảo tài khoản Admin mẫu trong bảng admins riêng biệt
            var sampleAdmin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Email == "admin@ieltshsk.com" || a.Username == "admin");
            if (sampleAdmin == null)
            {
                sampleAdmin = new Admin
                {
                    Username = "admin",
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@ieltshsk.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Aa@cuongnane"),
                    Role = "admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Admins.Add(sampleAdmin);
            }
            else
            {
                sampleAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Aa@cuongnane");
                sampleAdmin.Role = "admin";
                sampleAdmin.IsActive = true;
            }
            await dbContext.SaveChangesAsync();

            // Kiểm tra nhanh: Nếu LearningSections đã tồn tại, các bảng khởi tạo ban đầu khác đã đầy đủ
            bool alreadySeeded = await dbContext.LearningSections.AnyAsync();

            if (!alreadySeeded)
            {

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
                var author = await dbContext.Users.FirstOrDefaultAsync();
                if (author != null)
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
                        CreatedById = author.Id
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
                    new LearningSection { Name = "Từ vựng HSK", Description = "Từ vựng HSK 1-9", Icon = "bi-spellcheck", Route = "/hsk/tu-vung", Language = "HSK", OrderIndex = 2 },
                    new LearningSection { Name = "Bắn Từ Vựng", Description = "Gõ pinyin bắn từ vựng rơi", Icon = "bi-crosshair", Route = "/hsk/vocab-shooter", Language = "HSK", OrderIndex = 3 }
                );
                await dbContext.SaveChangesAsync();
            }
            // Dọn dẹp các mục Nghe HSK, Đọc HSK, Viết HSK, Nói HSK khỏi learning_sections
            var deprecatedHskRoutes = new[] { "/hsk/listening", "/hsk/reading", "/hsk/writing", "/hsk/speaking" };
            var deprecatedSections = await dbContext.LearningSections
                .Where(s => s.Language == "HSK" && deprecatedHskRoutes.Contains(s.Route))
                .ToListAsync();
            if (deprecatedSections.Any())
            {
                dbContext.LearningSections.RemoveRange(deprecatedSections);
                await dbContext.SaveChangesAsync();
            }

            // Đảm bảo các mục HSK còn lại có đúng thứ tự (OrderIndex: 1. Luyện đề, 2. Từ vựng)
            var luyenDeHsk = await dbContext.LearningSections.FirstOrDefaultAsync(s => s.Language == "HSK" && s.Route == "/hsk/luyen-de");
            if (luyenDeHsk != null && luyenDeHsk.OrderIndex != 1) { luyenDeHsk.OrderIndex = 1; }
            var tuVungHsk = await dbContext.LearningSections.FirstOrDefaultAsync(s => s.Language == "HSK" && s.Route == "/hsk/tu-vung");
            if (tuVungHsk != null && tuVungHsk.OrderIndex != 2) { tuVungHsk.OrderIndex = 2; }
            await dbContext.SaveChangesAsync();

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

            // Ensure Ngữ pháp exists for IELTS
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "IELTS" && (s.Route == "/ielts/grammar" || s.Route == "/ielts/ngu-phap")))
            {
                var uuTien = await dbContext.LearningSections.FirstOrDefaultAsync(s => s.Language == "IELTS" && s.Route == "/ielts/uu-tien");
                if (uuTien != null && uuTien.OrderIndex <= 6)
                {
                    uuTien.OrderIndex = 7;
                }

                dbContext.LearningSections.Add(
                    new LearningSection
                    {
                        Name = "Ngữ pháp",
                        Description = "Kho cấu trúc câu theo Band",
                        Icon = "bi-diagram-3",
                        Route = "/ielts/grammar",
                        Language = "IELTS",
                        OrderIndex = 6
                    }
                );
                await dbContext.SaveChangesAsync();
            }

            // Ensure Trò chơi (/games) exists for IELTS, HSK, TOEIC
            bool hasNewGames = false;
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "IELTS" && s.Route == "/games"))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Trò chơi", Description = "Game học từ & phản xạ", Icon = "bi-controller", Route = "/games", Language = "IELTS", OrderIndex = 10 }
                );
                hasNewGames = true;
            }
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "HSK" && s.Route == "/games"))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Trò chơi", Description = "Game học từ & phản xạ", Icon = "bi-controller", Route = "/games", Language = "HSK", OrderIndex = 10 }
                );
                hasNewGames = true;
            }
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "TOEIC" && s.Route == "/games"))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Trò chơi", Description = "Game học từ & phản xạ", Icon = "bi-controller", Route = "/games", Language = "TOEIC", OrderIndex = 10 }
                );
                hasNewGames = true;
            }
            if (hasNewGames)
            {
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
                DO $$
                BEGIN
                    -- Xóa bảng PascalCase cũ nếu đã tạo nhầm trước đó
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'GrammarStructures') THEN
                        DROP TABLE ""GrammarStructures"";
                    END IF;

                    -- Tạo bảng snake_case chuẩn EF Core Npgsql
                    CREATE TABLE IF NOT EXISTS grammar_structures (
                        id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        structure_code text NOT NULL,
                        band_level text NOT NULL,
                        category text NOT NULL,
                        grammar_topic text NOT NULL,
                        formula text NOT NULL,
                        usage_function text NOT NULL,
                        basic_example text,
                        advanced_example text NOT NULL,
                        vietnamese_meaning text NOT NULL,
                        key_collocations text,
                        common_mistakes text,
                        practice_exercise text,
                        tags text,
                        display_order integer NOT NULL DEFAULT 0,
                        is_active boolean NOT NULL DEFAULT TRUE,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone
                    );
                END $$;
            ");

            await GrammarSeedData.SeedGrammarStructuresAsync(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] GrammarStructures table creation note: {ex.Message}");
        }
    }
}

