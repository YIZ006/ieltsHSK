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

                    -- 6. Bảng toeic_vocabularies
                    CREATE TABLE IF NOT EXISTS toeic_vocabularies (
                        id SERIAL PRIMARY KEY,
                        word TEXT NOT NULL,
                        ipa TEXT NOT NULL DEFAULT '',
                        meaning TEXT NOT NULL,
                        example TEXT,
                        topic TEXT NOT NULL DEFAULT 'Khác',
                        is_custom BOOLEAN NOT NULL DEFAULT FALSE,
                        user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
                        is_active BOOLEAN NOT NULL DEFAULT TRUE,
                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );
                    CREATE INDEX IF NOT EXISTS ix_toeic_vocabularies_topic ON toeic_vocabularies(topic);

                    -- 7. Bảng toeic_vocabulary_progresses
                    CREATE TABLE IF NOT EXISTS toeic_vocabulary_progresses (
                        id SERIAL PRIMARY KEY,
                        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                        vocabulary_id INTEGER NOT NULL REFERENCES toeic_vocabularies(id) ON DELETE CASCADE,
                        status TEXT NOT NULL DEFAULT 'Learned',
                        learned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        CONSTRAINT uq_toeic_vocab_progress_user_vocab UNIQUE (user_id, vocabulary_id)
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

            // 2. Seed thông báo mẫu nếu bảng notifications chưa có dữ liệu
            if (!await dbContext.Notifications.AnyAsync())
            {
                dbContext.Notifications.AddRange(
                    new Notification
                    {
                        Title = "Chào mừng bạn đến với ieltsHSK!",
                        Message = "Chúc bạn có những giờ học tập và luyện thi hiệu quả với hệ thống đề thi chuẩn IELTS, TOEIC và HSK.",
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
                        Title = "Bộ đề thi TOEIC ETS mới đã sẵn sàng",
                        Message = "Luyện thi trọn bộ Listening & Reading chuẩn format quốc tế với đồng hồ bấm giờ và chấm điểm tự động.",
                        Type = "exam",
                        Icon = "bi-journal-check",
                        TargetUrl = "/toeic/test",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                        CreatedByAdmin = "Admin",
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
        // Seed TOEIC Vocabulary
        await ToeicVocabSeedData.SeedToeicVocabularyAsync(dbContext);

        // Seed Graded Reader Stories
        try
        {
            await Persistence.StorySeedData.SeedStoriesAsync(dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Story seeding note: {ex.Message}");
        }
    }
}

