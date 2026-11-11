using Backend.Application.Abstractions;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Text.Json;

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
        var redisConn = configuration["Redis:ConnectionString"] ?? "127.0.0.1:6379";
        var redisEnabled = configuration.GetValue<bool?>("Redis:Enabled") ?? true;

        if (redisEnabled && !string.IsNullOrWhiteSpace(redisConn))
        {
            var lazyMultiplexer = new Lazy<StackExchange.Redis.IConnectionMultiplexer>(() =>
            {
                try
                {
                    var options = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                    options.AbortOnConnectFail = false; // Resilience: Do not crash if Redis is unavailable
                    options.ConnectTimeout = 3000;
                    options.SyncTimeout = 3000;
                    options.AsyncTimeout = 3000;
                    var multiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(options);
                    if (multiplexer.IsConnected)
                    {
                        Console.WriteLine($"[Redis Success] Connected to Redis server ({redisConn}).");
                    }
                    return multiplexer;
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
        services.AddScoped<ICaptchaService, Backend.Infrastructure.Services.CloudflareTurnstileService>();
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
                        ALTER TABLE listen_videos DROP COLUMN IF EXISTS submitted_by_user_id;
                    END IF;

                    -- 3. Bảng lessons: dọn dẹp cột xp_reward
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'lessons') THEN
                        ALTER TABLE lessons DROP COLUMN IF EXISTS xp_reward;
                    END IF;

                    -- Dọn dẹp bảng mồ côi learning_resources (đã thay bằng websites, categories, languages)
                    DROP TABLE IF EXISTS learning_resources;

                    -- 4. Bảng ielts_vocabulary_progresses
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

                    -- 5. Bảng users: thêm các cột cá nhân hoá & mục tiêu
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'users') THEN
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS avatar_color TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS bio TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS target_exam TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS target_score TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS target_deadline TIMESTAMPTZ;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS ielts_level TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS hsk_level TEXT;
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS username_changed_at TIMESTAMPTZ;
                        ALTER TABLE users DROP COLUMN IF EXISTS xp;
                    END IF;

                    -- 5. Bảng user_study_activities
                    CREATE TABLE IF NOT EXISTS user_study_activities (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        activity_date TIMESTAMPTZ NOT NULL,
                        study_seconds INTEGER NOT NULL DEFAULT 0,
                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        CONSTRAINT uq_user_study_activities_user_date UNIQUE (user_id, activity_date)
                    );

                    -- 8. Bảng user_game_progresses
                    CREATE TABLE IF NOT EXISTS user_game_progresses (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        game_type TEXT NOT NULL,
                        level TEXT NOT NULL,
                        current_stage INTEGER NOT NULL DEFAULT 0,
                        max_unlocked_stage INTEGER NOT NULL DEFAULT 0,
                        high_score INTEGER NOT NULL DEFAULT 0,
                        updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        CONSTRAINT uq_user_game_progresses_user_game_lvl UNIQUE (user_id, game_type, level)
                    );

                    -- 9. Bảng exam_checkpoints
                    CREATE TABLE IF NOT EXISTS exam_checkpoints (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
                        user_identifier TEXT NOT NULL,
                        skill TEXT NOT NULL,
                        exam_url TEXT NOT NULL,
                        mock_test_id INTEGER,
                        checkpoint_data_json JSONB NOT NULL,
                        seconds_remaining INTEGER NOT NULL DEFAULT 0,
                        last_saved_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );
                    CREATE INDEX IF NOT EXISTS ix_exam_checkpoints_lookup ON exam_checkpoints (user_identifier, skill, exam_url);

                    -- 10. Bảng notifications & user_notification_reads
                    CREATE TABLE IF NOT EXISTS notifications (
                        id SERIAL PRIMARY KEY,
                        title TEXT NOT NULL,
                        message TEXT NOT NULL,
                        type TEXT NOT NULL DEFAULT 'system',
                        icon TEXT NOT NULL DEFAULT 'bi-bell-fill',
                        target_url TEXT,
                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        created_by_admin TEXT,
                        is_broadcast BOOLEAN NOT NULL DEFAULT TRUE,
                        user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
                        is_active BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    CREATE INDEX IF NOT EXISTS ix_notifications_created_at ON notifications (created_at DESC);

                    CREATE TABLE IF NOT EXISTS user_notification_reads (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        notification_id INTEGER NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
                        read_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        CONSTRAINT uq_user_notif_read UNIQUE (user_id, notification_id)
                    );
                    CREATE INDEX IF NOT EXISTS ix_user_notif_reads_user ON user_notification_reads (user_id);

                    -- 11. Bảng hsk_review_words
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'hsk_vocabularies') THEN
                        CREATE TABLE IF NOT EXISTS hsk_review_words (
                            id SERIAL PRIMARY KEY,
                            user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                            vocabulary_id INTEGER NOT NULL REFERENCES hsk_vocabularies(id) ON DELETE CASCADE,
                            added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                            source TEXT,
                            CONSTRAINT uq_hsk_review_words_user_vocab UNIQUE (user_id, vocabulary_id)
                        );
                        CREATE INDEX IF NOT EXISTS ix_hsk_review_words_user ON hsk_review_words (user_id);
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
            // 1. Đảm bảo tài khoản Admin trong bảng admins (không ghi đè nếu đã tồn tại)
            var sampleAdmin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Email == "admin@ieltshsk.com" || a.Username == "admin");
            if (sampleAdmin == null)
            {
                var initialPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD");
                bool isGenerated = false;
                if (string.IsNullOrWhiteSpace(initialPassword))
                {
                    // Sinh mật khẩu ngẫu nhiên an toàn nếu không truyền qua biến môi trường
                    initialPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(15))
                        .Replace("+", "@").Replace("/", "$")[..14] + "!Aa1";
                    isGenerated = true;
                }

                sampleAdmin = new Admin
                {
                    Username = "admin",
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@ieltshsk.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                    Role = "admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Admins.Add(sampleAdmin);
                await dbContext.SaveChangesAsync();

                if (isGenerated)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[SECURITY NOTICE] Initial admin created with generated password: {initialPassword}");
                    Console.WriteLine("[SECURITY NOTICE] Please login and change this password immediately!");
                    Console.ResetColor();
                }
            }
            // LƯU Ý BẢO MẬT: Nếu admin đã tồn tại, tuyệt đối giữ nguyên PasswordHash của họ!

            // 2. Seed thông báo mẫu nếu bảng notifications chưa có dữ liệu
            if (!await dbContext.Notifications.AnyAsync())
            {
                dbContext.Notifications.AddRange(
                    new Notification
                    {
                        Title = "Chào mừng bạn đến với ieltsHSK!",
                        Message = "Chúc bạn có những giờ học tập và luyện thi hiệu quả với hệ thống đề thi chuẩn IELTS và HSK.",
                        Type = "system",
                        Icon = "bi-stars",
                        TargetUrl = "/",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAdmin = "Hệ thống",
                        IsBroadcast = true,
                        IsActive = true
                    },
                    new Notification
                    {
                        Title = "Huy hiệu & Thành tích trong Trang cá nhân",
                        Message = "Tính năng theo dõi tiến độ và huy hiệu đã được tích hợp trực tiếp vào hồ sơ học viên.",
                        Type = "update",
                        Icon = "bi-trophy-fill",
                        TargetUrl = "",
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        CreatedByAdmin = "Admin",
                        IsBroadcast = true,
                        IsActive = true
                    }
                );
                await dbContext.SaveChangesAsync();
            }

            // Kiểm tra nhanh: Nếu LearningSections đã tồn tại, các bảng khởi tạo ban đầu khác đã đầy đủ
            bool alreadySeeded = await dbContext.LearningSections.AnyAsync();

            if (!alreadySeeded)
            {
                // Seed LearningSections
            if (!await dbContext.LearningSections.AnyAsync())
            {
                dbContext.LearningSections.AddRange(
                    new LearningSection { Name = "Phòng thi thử", Description = "Chưa thử — bắt đầu góc này?", Icon = "bi-journal-text", Route = "/ielts/mock-test", Language = "IELTS", OrderIndex = 1 },
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

            // Đồng bộ mục IELTS: Đổi "Luyện đề" thành "Phòng thi thử" (/ielts/mock-test)
            var luyenDeIelts = await dbContext.LearningSections.FirstOrDefaultAsync(s => s.Language == "IELTS" && (s.Route == "/ielts/luyen-de" || s.Route == "/ielts/phong-thi-thu" || s.Route == "/ielts/mock-test" || s.Route == "/ielts/mock-tests" || s.Name == "Luyện đề"));
            if (luyenDeIelts != null)
            {
                luyenDeIelts.Name = "Phòng thi thử";
                luyenDeIelts.Route = "/ielts/mock-test";
                luyenDeIelts.OrderIndex = 1;
                await dbContext.SaveChangesAsync();
            }

            // Seed IELTS MockTests
            if (!await dbContext.MockTests.AnyAsync(m => m.Title == "Practise Test 1"))
            {
                dbContext.MockTests.Add(new MockTest
                {
                    CollectionName = "IELTS Mock Test 2025 December",
                    Title = "Practise Test 1",
                    ListeningUrl = "sample-data/listening-test-1.json",
                    ReadingUrl = "sample-data/reading-test-v2.json",
                    WritingUrl = "sample-data/writing-test-1.json",
                    SpeakingUrl = "sample-data/speaking-test-1.json",
                    ListeningAnswerUrl = "sample-data/IELTS_Mock_Test_2025_December_Listening_Practise_Test_1_IELTS_Online_Tests.answers.json",
                    ReadingAnswerUrl = "sample-data/IELTS_Mock_Test_2025_December_Reading_Practise_Test_1_IELTS_Online_Tests.answers.json",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }

            if (!await dbContext.MockTests.AnyAsync(m => m.Title == "Actual Test 1" && m.CollectionName == "IELTS Recent Actual Tests Vol 1"))
            {
                dbContext.MockTests.Add(new MockTest
                {
                    CollectionName = "IELTS Recent Actual Tests Vol 1",
                    Title = "Actual Test 1",
                    ListeningUrl = "sample-data/listening-actual-vol1-test1.json",
                    ListeningAnswerUrl = "sample-data/listening-actual-vol1-test1.answers.json",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
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

            // Ensure Trò chơi (/games) exists for IELTS, HSK
            bool hasNewGames = false;
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "IELTS" && s.Route == "/games"))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Trò chơi", Description = "Game học từ & phản xạ", Icon = "bi-controller", Route = "/games", Language = "IELTS", OrderIndex = 10 }
                );
                hasNewGames = true;
            }
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "HSK" && (s.Route == "/hsk/ngu-phap" || s.Route == "/hsk/grammar")))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Ngữ pháp HSK", Description = "Điểm ngữ pháp, chủ đề & ví dụ HSK 1–6", Icon = "bi-diagram-3", Route = "/hsk/ngu-phap", Language = "HSK", OrderIndex = 4 }
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
            if (!await dbContext.LearningSections.AnyAsync(s => s.Language == "HSK" && s.Route == "/hsk/on-tap"))
            {
                dbContext.LearningSections.Add(
                    new LearningSection { Name = "Ôn tập từ vựng", Description = "Sổ tay từ cần nhớ & flashcard", Icon = "bi-arrow-repeat", Route = "/hsk/on-tap", Language = "HSK", OrderIndex = 7 }
                );
                hasNewGames = true;
            }
            if (hasNewGames)
            {
                await dbContext.SaveChangesAsync();
            }

            // Invalidate navigation cache so changes are immediately served
            var cacheService = scope.ServiceProvider.GetService<Backend.Application.Abstractions.ICacheService>();
            if (cacheService != null)
            {
                try
                {
                    await cacheService.RemoveAsync("navigation:all");
                    await cacheService.RemoveAsync("navigation:IELTS");
                }
                catch { }
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
        // Seed Graded Reader Stories
        try
        {
            await Persistence.StorySeedData.SeedStoriesAsync(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Story seeding note: {ex.Message}");
        }

        // Seed Culture Stories, Slang Vocabs & Exam Schedules tables & sample data
        try
        {
            await SeedCultureAndExamDataAsync(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Culture & Exam seed note: {ex.Message}");
        }
    }

    private static async Task SeedCultureAndExamDataAsync(AppDbContext dbContext)
    {
        // 1. Ensure PostgreSQL schema compatibility
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    -- 1. exam_schedules
                    CREATE TABLE IF NOT EXISTS exam_schedules (
                        id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        certificate_type text NOT NULL,
                        level text,
                        exam_date timestamp with time zone NOT NULL,
                        registration_deadline timestamp with time zone NOT NULL,
                        location text NOT NULL,
                        city text NOT NULL,
                        format text NOT NULL DEFAULT 'Computer',
                        fee_vnd numeric(18, 2) NOT NULL DEFAULT 0,
                        organizer text NOT NULL,
                        description text,
                        registration_url text,
                        status text NOT NULL DEFAULT 'RegistrationOpen',
                        is_active boolean NOT NULL DEFAULT TRUE,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone
                    );
                    CREATE INDEX IF NOT EXISTS ix_exam_schedules_certificate_type_exam_date ON exam_schedules (certificate_type, exam_date);
                    CREATE INDEX IF NOT EXISTS ix_exam_schedules_city ON exam_schedules (city);
                    CREATE INDEX IF NOT EXISTS ix_exam_schedules_organizer ON exam_schedules (organizer);
                    CREATE INDEX IF NOT EXISTS ix_exam_schedules_registration_deadline ON exam_schedules (registration_deadline);

                    -- 2. culture_stories
                    CREATE TABLE IF NOT EXISTS culture_stories (
                        id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        title_zh text NOT NULL,
                        title_vi text NOT NULL,
                        pinyin text NOT NULL,
                        slug text NOT NULL,
                        category text NOT NULL DEFAULT 'chengyu',
                        level text NOT NULL DEFAULT 'HSK 3',
                        summary_vi text,
                        content_zh text NOT NULL,
                        content_vi text NOT NULL,
                        paragraphs_json jsonb NOT NULL DEFAULT '[]'::jsonb,
                        vocabulary_json jsonb NOT NULL DEFAULT '[]'::jsonb,
                        questions_json jsonb NOT NULL DEFAULT '[]'::jsonb,
                        audio_url text,
                        thumbnail_url text,
                        estimated_minutes integer NOT NULL DEFAULT 3,
                        views_count integer NOT NULL DEFAULT 0,
                        is_published boolean NOT NULL DEFAULT TRUE,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_culture_stories_slug ON culture_stories (slug);
                    CREATE INDEX IF NOT EXISTS ix_culture_stories_category ON culture_stories (category);
                    CREATE INDEX IF NOT EXISTS ix_culture_stories_level ON culture_stories (level);
                    CREATE INDEX IF NOT EXISTS ix_culture_stories_created_at ON culture_stories (created_at);

                    -- 3. slang_vocabs
                    CREATE TABLE IF NOT EXISTS slang_vocabs (
                        id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        term text NOT NULL,
                        pinyin text NOT NULL,
                        meaning_vi text NOT NULL,
                        category text NOT NULL DEFAULT 'slang_trend',
                        origin text,
                        dialogue_example_zh text,
                        dialogue_example_vi text,
                        audio_url text,
                        popularity_score integer NOT NULL DEFAULT 90,
                        display_order integer NOT NULL DEFAULT 0,
                        is_active boolean NOT NULL DEFAULT TRUE,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_slang_vocabs_term ON slang_vocabs (term);
                    CREATE INDEX IF NOT EXISTS ix_slang_vocabs_category ON slang_vocabs (category);
                    CREATE INDEX IF NOT EXISTS ix_slang_vocabs_popularity_score ON slang_vocabs (popularity_score);
                    CREATE INDEX IF NOT EXISTS ix_slang_vocabs_display_order ON slang_vocabs (display_order);
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Schema creation note for exam/culture/slang: {ex.Message}");
        }

        // 2. Resolve sample-data path
        static string FindSampleFile(string fileName)
        {
            var searchDirs = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "sample-data"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sample-data"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend", "src", "Frontend.App", "wwwroot", "sample-data"),
                Path.Combine(Directory.GetCurrentDirectory(), "frontend", "src", "Frontend.App", "wwwroot", "sample-data"),
                Path.Combine("D:", "project", "ieltsHSK", "frontend", "src", "Frontend.App", "wwwroot", "sample-data")
            };

            foreach (var dir in searchDirs)
            {
                var candidate = Path.Combine(dir, fileName);
                if (File.Exists(candidate)) return candidate;
            }
            return string.Empty;
        }

        // 3. Seed Exam Schedules
        try
        {
            if (!await dbContext.ExamSchedules.AnyAsync())
            {
                var filePath = FindSampleFile("exam-schedules-2026.json");
                if (string.IsNullOrEmpty(filePath)) filePath = FindSampleFile("exam-schedules.json");

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    using var doc = JsonDocument.Parse(json);
                    var list = new List<ExamSchedule>();
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var certType = elem.TryGetProperty("certificateType", out var ctp) ? ctp.GetString() ?? "HSK" : "HSK";
                        var level = elem.TryGetProperty("level", out var lp) ? lp.GetString() : null;
                        var examDateStr = elem.TryGetProperty("examDate", out var edp) ? edp.GetString() : null;
                        var regDeadlineStr = elem.TryGetProperty("registrationDeadline", out var rdp) ? rdp.GetString() : null;
                        var location = elem.TryGetProperty("location", out var locp) ? locp.GetString() : null;
                        if (string.IsNullOrEmpty(location) && elem.TryGetProperty("testCenter", out var tcp))
                        {
                            location = tcp.GetString();
                        }
                        var city = elem.TryGetProperty("city", out var cip) ? cip.GetString() ?? "Hà Nội" : "Hà Nội";
                        var format = elem.TryGetProperty("format", out var fmp) ? fmp.GetString() ?? "Computer" : "Computer";
                        var feeVnd = elem.TryGetProperty("feeVnd", out var fvp) ? fvp.GetDecimal() : 0m;
                        var organizer = elem.TryGetProperty("organizer", out var ogp) ? ogp.GetString() ?? "" : "";
                        var desc = elem.TryGetProperty("description", out var dp) ? dp.GetString() : null;
                        var regUrl = elem.TryGetProperty("registrationUrl", out var rup) ? rup.GetString() : null;
                        var status = elem.TryGetProperty("status", out var stp) ? stp.GetString() ?? "RegistrationOpen" : "RegistrationOpen";

                        DateTime examDate = DateTime.TryParse(examDateStr, out var ed) ? DateTime.SpecifyKind(ed, DateTimeKind.Utc) : DateTime.UtcNow.AddMonths(1);
                        DateTime regDeadline = DateTime.TryParse(regDeadlineStr, out var rd) ? DateTime.SpecifyKind(rd, DateTimeKind.Utc) : examDate.AddDays(-14);

                        list.Add(new ExamSchedule
                        {
                            CertificateType = certType,
                            Level = level,
                            ExamDate = examDate,
                            RegistrationDeadline = regDeadline,
                            Location = location ?? "Địa điểm thi toàn quốc",
                            City = city,
                            Format = format,
                            FeeVnd = feeVnd,
                            Organizer = organizer,
                            Description = desc,
                            RegistrationUrl = regUrl,
                            Status = status,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (list.Count > 0)
                    {
                        dbContext.ExamSchedules.AddRange(list);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"[SeedData] Seeded {list.Count} exam schedules from JSON.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] ExamSchedule seed note: {ex.Message}");
        }

        // 4. Seed Culture Stories
        try
        {
            if (!await dbContext.CultureStories.AnyAsync())
            {
                var filePath = FindSampleFile("hsk-culture-stories.json");
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    using var doc = JsonDocument.Parse(json);
                    var list = new List<CultureStory>();
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var titleZh = elem.TryGetProperty("titleZh", out var tzp) ? tzp.GetString() ?? "" : "";
                        var titleVi = elem.TryGetProperty("titleVi", out var tvp) ? tvp.GetString() ?? "" : "";
                        var pinyin = elem.TryGetProperty("pinyin", out var pyp) ? pyp.GetString() ?? "" : "";
                        var slug = elem.TryGetProperty("slug", out var slp) ? slp.GetString() : null;
                        if (string.IsNullOrWhiteSpace(slug))
                        {
                            slug = System.Text.RegularExpressions.Regex.Replace(titleVi.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-");
                            if (string.IsNullOrWhiteSpace(slug)) slug = "story-" + Guid.NewGuid().ToString()[..8];
                        }
                        var category = elem.TryGetProperty("category", out var cgp) ? cgp.GetString() ?? "chengyu" : "chengyu";
                        var level = elem.TryGetProperty("level", out var lvp) ? lvp.GetString() ?? "HSK 3" : "HSK 3";
                        var summaryVi = elem.TryGetProperty("summaryVi", out var smp) ? smp.GetString() : null;
                        var contentZh = elem.TryGetProperty("contentZh", out var czp) ? czp.GetString() ?? "" : "";
                        var contentVi = elem.TryGetProperty("contentVi", out var cvp) ? cvp.GetString() ?? "" : "";

                        string paragraphsJson = "[]";
                        if (elem.TryGetProperty("paragraphs", out var prp))
                            paragraphsJson = prp.GetRawText();
                        else if (elem.TryGetProperty("sentences", out var snp))
                            paragraphsJson = snp.GetRawText();

                        string vocabJson = "[]";
                        if (elem.TryGetProperty("vocabulary", out var vcp))
                            vocabJson = vcp.GetRawText();
                        else if (elem.TryGetProperty("vocabularyJson", out var vjp))
                            vocabJson = vjp.GetString() ?? "[]";

                        string questionsJson = "[]";
                        if (elem.TryGetProperty("questions", out var qsp))
                            questionsJson = qsp.GetRawText();
                        else if (elem.TryGetProperty("questionsJson", out var qjp))
                            questionsJson = qjp.GetString() ?? "[]";

                        var audioUrl = elem.TryGetProperty("audioUrl", out var aup) ? aup.GetString() : null;
                        var thumbUrl = elem.TryGetProperty("thumbnailUrl", out var tmp) ? tmp.GetString() : null;
                        var estMins = elem.TryGetProperty("estimatedMinutes", out var emp) ? emp.GetInt32() : 3;

                        list.Add(new CultureStory
                        {
                            TitleZh = titleZh,
                            TitleVi = titleVi,
                            Pinyin = pinyin,
                            Slug = slug,
                            Category = category,
                            Level = level,
                            SummaryVi = summaryVi,
                            ContentZh = contentZh,
                            ContentVi = contentVi,
                            ParagraphsJson = paragraphsJson,
                            VocabularyJson = vocabJson,
                            QuestionsJson = questionsJson,
                            AudioUrl = audioUrl,
                            ThumbnailUrl = thumbUrl,
                            EstimatedMinutes = estMins,
                            IsPublished = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (list.Count > 0)
                    {
                        dbContext.CultureStories.AddRange(list);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"[SeedData] Seeded {list.Count} culture stories from JSON.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] CultureStory seed note: {ex.Message}");
        }

        // 5. Seed Slang Vocabs
        try
        {
            if (!await dbContext.SlangVocabs.AnyAsync())
            {
                var filePath = FindSampleFile("hsk-genz-slang.json");
                if (string.IsNullOrEmpty(filePath)) filePath = FindSampleFile("hsk-slang-vocabs.json");

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    using var doc = JsonDocument.Parse(json);
                    var list = new List<SlangVocab>();
                    int order = 1;
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var term = elem.TryGetProperty("term", out var tmp) ? tmp.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(term)) continue;

                        var pinyin = elem.TryGetProperty("pinyin", out var pyp) ? pyp.GetString() ?? "" : "";
                        var meaningVi = elem.TryGetProperty("meaningVi", out var mvp) ? mvp.GetString() ?? "" : "";
                        var category = elem.TryGetProperty("category", out var ctp) ? ctp.GetString() ?? "slang_trend" : "slang_trend";
                        var origin = elem.TryGetProperty("origin", out var ogp) ? ogp.GetString() : null;

                        var dialZh = elem.TryGetProperty("dialogueExampleZh", out var dzp) ? dzp.GetString() : null;
                        if (string.IsNullOrEmpty(dialZh) && elem.TryGetProperty("dialogueZh", out var dzhp))
                        {
                            dialZh = dzhp.GetString();
                        }

                        var dialVi = elem.TryGetProperty("dialogueExampleVi", out var dvp) ? dvp.GetString() : null;
                        if (string.IsNullOrEmpty(dialVi) && elem.TryGetProperty("dialogueVi", out var dvip))
                        {
                            dialVi = dvip.GetString();
                        }

                        var audioUrl = elem.TryGetProperty("audioUrl", out var aup) ? aup.GetString() : null;
                        var popScore = elem.TryGetProperty("popularityScore", out var psp) ? psp.GetInt32() : 90;

                        list.Add(new SlangVocab
                        {
                            Term = term,
                            Pinyin = pinyin,
                            MeaningVi = meaningVi,
                            Category = category,
                            Origin = origin,
                            DialogueExampleZh = dialZh,
                            DialogueExampleVi = dialVi,
                            AudioUrl = audioUrl,
                            PopularityScore = popScore,
                            DisplayOrder = order++,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (list.Count > 0)
                    {
                        dbContext.SlangVocabs.AddRange(list);
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"[SeedData] Seeded {list.Count} slang vocabs from JSON.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] SlangVocab seed note: {ex.Message}");
        }

        // 6. Seed HSK Vocabularies from JSON if table is nearly empty (< 100 rows)
        try
        {
            var vocabCount = await dbContext.HskVocabularies.CountAsync();
            if (vocabCount < 100)
            {
                var hskFiles = new[] { "hsk/hsk1.json", "hsk/hsk2.json", "hsk/hsk3.json", "hsk/hsk4.json", "hsk/hsk5.json", "hsk/hsk6.json" };
                var existingSet = (await dbContext.HskVocabularies
                    .Select(v => v.HskLevel + "_" + v.Hanzi)
                    .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newVocabs = new List<HskVocabulary>();
                foreach (var hskRelPath in hskFiles)
                {
                    var filePath = FindSampleFile(hskRelPath);
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        using var doc = JsonDocument.Parse(json);
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            var hanzi = elem.TryGetProperty("hanzi", out var hz) ? hz.GetString() ?? "" : "";
                            if (string.IsNullOrWhiteSpace(hanzi)) continue;
                            var level = elem.TryGetProperty("hskLevel", out var lv) ? lv.GetString() ?? "HSK1" : "HSK1";
                            level = level.Trim().ToUpperInvariant();

                            var key = level + "_" + hanzi.Trim();
                            if (existingSet.Contains(key)) continue;

                            var pinyin = elem.TryGetProperty("pinyin", out var py) ? py.GetString() ?? "" : "";
                            var meaning = elem.TryGetProperty("meaning", out var mn) ? mn.GetString() ?? "" : "";
                            var wordType = elem.TryGetProperty("wordType", out var wt) ? wt.GetString() : null;
                            var exSentence = elem.TryGetProperty("exampleSentence", out var es) ? es.GetString() : null;
                            var exPinyin = elem.TryGetProperty("examplePinyin", out var ep) ? ep.GetString() : null;
                            var exMeaning = elem.TryGetProperty("exampleMeaning", out var em) ? em.GetString() : null;
                            var audioUrl = elem.TryGetProperty("audioUrl", out var au) ? au.GetString() : null;
                            var order = elem.TryGetProperty("displayOrder", out var ord) ? ord.GetInt32() : 1;

                            newVocabs.Add(new HskVocabulary
                            {
                                HskLevel = level,
                                Hanzi = hanzi.Trim(),
                                Pinyin = pinyin.Trim(),
                                Meaning = meaning.Trim(),
                                WordType = wordType?.Trim(),
                                ExampleSentence = exSentence?.Trim(),
                                ExamplePinyin = exPinyin?.Trim(),
                                ExampleMeaning = exMeaning?.Trim(),
                                AudioUrl = audioUrl?.Trim(),
                                DisplayOrder = order,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            existingSet.Add(key);
                        }
                    }
                }

                if (newVocabs.Count > 0)
                {
                    dbContext.HskVocabularies.AddRange(newVocabs);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"[SeedData] Seeded {newVocabs.Count} HSK vocabularies from JSON.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] HskVocabulary seed note: {ex.Message}");
        }
    }
}

