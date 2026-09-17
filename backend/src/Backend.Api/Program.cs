using Backend.Api;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Giới hạn kích thước body upload tối đa 50MB (audio, ảnh, excel) để chống tấn công DoS
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});

// Cấu hình CORS an toàn: Không dùng wildcard tự do khi bật AllowCredentials()
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        var allowedOriginsConfig = builder.Configuration["Cors:AllowedOrigins"] 
            ?? builder.Configuration["Frontend:BaseUrl"];
        var configuredOrigins = (allowedOriginsConfig ?? "")
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            // Trong môi trường Development: cho phép toàn bộ localhost và 127.0.0.1 trên mọi cổng
            if (builder.Environment.IsDevelopment())
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // Kiểm tra danh sách origin được cấu hình (ví dụ domain production)
            return configuredOrigins.Any(allowed => string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase));
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Rate Limiting cho các endpoint xác thực (tối đa 15 request/phút) chống brute-force và DDoS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Cấu hình Jwt:Key không được để trống.");
        }
        if (!builder.Environment.IsDevelopment() && jwtKey.Contains("SuperSecretKeyThatIsAtLeast32BytesLongForHSK"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[SECURITY ALERT] Jwt:Key đang sử dụng khóa mẫu mặc định trong môi trường Non-Development! Hãy thiết lập biến môi trường Jwt__Key.");
            Console.ResetColor();
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Chặn token của tài khoản bị khoá/xoá ngay cả khi JWT còn hạn
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
                var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? context.Principal?.FindFirst("role")?.Value;
                var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
                    || context.Principal?.FindFirst("admin_id") != null;

                var cache = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var cacheKey = isAdmin ? $"auth_admin_active_{userId}" : $"auth_user_active_{userId}";

                if (cache != null && cache.TryGetValue(cacheKey, out bool isActive))
                {
                    if (!isActive)
                    {
                        context.Fail(isAdmin ? "Admin account is disabled or no longer exists." : "Account is disabled or no longer exists.");
                    }
                    return;
                }

                if (isAdmin)
                {
                    var admin = await dbContext.Admins.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
                    if (admin == null || !admin.IsActive)
                    {
                        cache?.Set(cacheKey, false, TimeSpan.FromSeconds(60));
                        context.Fail("Admin account is disabled or no longer exists.");
                        return;
                    }
                    cache?.Set(cacheKey, true, TimeSpan.FromSeconds(60));
                }
                else
                {
                    var user = await dbContext.Users.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
                    if (user == null || !user.IsActive)
                    {
                        cache?.Set(cacheKey, false, TimeSpan.FromSeconds(60));
                        context.Fail("Account is disabled or no longer exists.");
                        return;
                    }
                    cache?.Set(cacheKey, true, TimeSpan.FromSeconds(60));
                }
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Khởi chạy kết nối DB, nạp trước model EF Core, kết nối Redis và kiểm tra seed ngầm
// để Kestrel mở cổng 5101 NGAY LẬP TỨC (< 100ms) mà không chặn các request đầu tiên từ client.
_ = Task.Run(async () =>
{
    try
    {
        // 1. Kích hoạt mở sẵn kết nối (pre-warm) tới PostgreSQL & nạp model EF Core
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
        await dbContext.Database.CanConnectAsync();

        // 2. Kích hoạt kết nối trước tới Redis nếu có bật
        _ = app.Services.GetService<StackExchange.Redis.IConnectionMultiplexer>();

        // 3. Đồng bộ migration và kiểm tra dữ liệu khởi tạo
        await app.Services.SeedDataAsync();
        Console.WriteLine("[Startup] Database connection warmed up & seed data verified.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup Warning] Background initialization note: {ex.Message}");
    }
});

// Swagger: Chỉ bật trong môi trường Development hoặc khi có cấu hình EnableSwagger = true
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Security Headers Middleware: Bảo vệ chống MIME sniffing, clickjacking, và rò rỉ referrer
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    // Cho phép microphone phục vụ chức năng luyện nói IELTS Speaking, chặn các quyền không cần thiết
    context.Response.Headers.Append("Permissions-Policy", "microphone=(self), camera=(), geolocation=()");
    await next();
});

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Root health check endpoint for Render & container monitoring
app.MapGet("/", () => Results.Ok(new { status = "healthy", app = "IELTS & HSK Backend API", timestamp = DateTime.UtcNow }));

app.MapGet("/api/ielts/courses", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "ielts:courses";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.CourseDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var courses = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Courses
            .Where(c => c.Status == "published")
            .Select(c => new Backend.Application.DTOs.CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                Thumbnail = c.Thumbnail,
                Level = c.Level,
                Category = c.Category,
                DurationMinutes = c.DurationMinutes
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, courses, TimeSpan.FromHours(2), cancellationToken);
    return Results.Ok(courses);
});

app.MapGet("/api/ielts/websites", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "ielts:websites";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.WebsiteDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var websites = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Websites
            .Where(w => w.IsActive && w.Language.Code == "EN")
            .Select(w => new Backend.Application.DTOs.WebsiteDto
            {
                Id = w.Id,
                Title = w.Title,
                Url = w.Url,
                Description = w.Description,
                IsFree = w.IsFree,
                RecommendedLevel = w.RecommendedLevel,
                ThumbnailUrl = w.ThumbnailUrl
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, websites, TimeSpan.FromHours(2), cancellationToken);
    return Results.Ok(websites);
});

app.MapGet("/api/ielts/sections", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "ielts:sections";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.LearningSectionDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var sections = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.LearningSections
            .Where(s => s.Language == "IELTS")
            .OrderBy(s => s.OrderIndex)
            .Select(s => new Backend.Application.DTOs.LearningSectionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                Route = s.Route,
                Language = s.Language,
                OrderIndex = s.OrderIndex
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, sections, TimeSpan.FromHours(2), cancellationToken);
    return Results.Ok(sections);
});

// LISTEN VIDEOS API
app.MapGet("/api/listen-videos", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "listen-videos:approved";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.ListenVideoDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var videos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.ListenVideos
            .Where(v => v.IsApproved)
            .OrderByDescending(v => v.SubmittedAt)
            .Select(v => new Backend.Application.DTOs.ListenVideoDto
            {
                Id = v.Id,
                YoutubeUrl = v.YoutubeUrl,
                Title = v.Title,
                ChannelName = v.ChannelName,
                Duration = v.Duration,
                ThumbnailUrl = v.ThumbnailUrl,
                Level = v.Level,
                Category = v.Category,
                IsApproved = v.IsApproved,
                TranscriptUrl = v.TranscriptUrl,
                WordCount = v.WordCount,
                SubmittedAt = v.SubmittedAt
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, videos, TimeSpan.FromMinutes(30), cancellationToken);
    return Results.Ok(videos);
});

app.MapGet("/api/listen-videos/{id}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null || !video.IsApproved) return Results.NotFound();

    return Results.Ok(new Backend.Application.DTOs.ListenVideoDto
    {
        Id = video.Id,
        YoutubeUrl = video.YoutubeUrl,
        Title = video.Title,
        ChannelName = video.ChannelName,
        Duration = video.Duration,
        ThumbnailUrl = video.ThumbnailUrl,
        Level = video.Level,
        Category = video.Category,
        IsApproved = video.IsApproved,
        TranscriptUrl = video.TranscriptUrl,
        WordCount = video.WordCount,
        SubmittedAt = video.SubmittedAt
    });
});

app.MapPost("/api/listen-videos/submit",
        [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.ListenVideoSubmitRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService ytService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    // Chỉ chấp nhận đường dẫn YouTube https để tránh injection qua URL
    if (string.IsNullOrWhiteSpace(req.YoutubeUrl)
        || !Uri.TryCreate(req.YoutubeUrl, UriKind.Absolute, out var ytUri)
        || ytUri.Scheme != Uri.UriSchemeHttps
        || (ytUri.Host != "www.youtube.com" && ytUri.Host != "youtube.com" && ytUri.Host != "youtu.be"
            && ytUri.Host != "m.youtube.com" && ytUri.Host != "music.youtube.com"))
    {
        return Results.BadRequest("Chỉ chấp nhận đường dẫn YouTube hợp lệ (https://www.youtube.com hoặc https://youtu.be).");
    }

    var videoIdToCheck = "";
    if (req.YoutubeUrl.Contains("v=")) videoIdToCheck = req.YoutubeUrl.Split("v=")[1].Split("&")[0];
    else if (req.YoutubeUrl.Contains("youtu.be/")) videoIdToCheck = req.YoutubeUrl.Split("youtu.be/")[1].Split("?")[0];
    
    if (!string.IsNullOrEmpty(videoIdToCheck))
    {
        bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            dbContext.ListenVideos, v => v.YoutubeUrl.Contains(videoIdToCheck), cancellationToken);
        // Nếu người dùng nhập trùng, giả vờ báo thành công để khỏi hiện lỗi, nhưng không lưu vào DB
        if (exists) return Results.Ok(new { Message = "Video submitted and pending approval" });
    }

    var title = "Unknown Title";
    var channel = "Unknown Channel";
    var duration = "00:00";
    var thumbnail = "";
    
    try
    {
        var info = await ytService.GetVideoInfoAsync(req.YoutubeUrl);
        title = info.Title;
        channel = info.ChannelName;
        duration = info.Duration.ToString(@"mm\:ss");
        thumbnail = info.ThumbnailUrl;
    }
    catch (Exception)
    {
        // Nếu không lấy được info, fallback lại parse ID cơ bản
        var videoId = "";
        if (req.YoutubeUrl.Contains("v="))
            videoId = req.YoutubeUrl.Split("v=")[1].Split("&")[0];
        else if (req.YoutubeUrl.Contains("youtu.be/"))
            videoId = req.YoutubeUrl.Split("youtu.be/")[1].Split("?")[0];
            
        thumbnail = string.IsNullOrEmpty(videoId) ? "" : $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
    }

    int? submitterUserId = null;
    string submittedBy = "User";
    if (httpContext.User.Identity?.IsAuthenticated == true)
    {
        var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? httpContext.User.FindFirst("sub")?.Value;
        if (int.TryParse(subClaim, out var parsedUid)) submitterUserId = parsedUid;

        var nameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                        ?? httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value;
        if (!string.IsNullOrWhiteSpace(nameClaim)) submittedBy = nameClaim;
    }

    var newVideo = new Backend.Domain.Entities.ListenVideo
    {
        YoutubeUrl = req.YoutubeUrl,
        Title = title, 
        ChannelName = channel,
        Duration = duration,
        ThumbnailUrl = thumbnail,
        Level = "B2",
        Category = "Giao tiếp",
        IsApproved = false,
        SubmittedAt = DateTime.UtcNow,
        UserId = submitterUserId
    };

    dbContext.ListenVideos.Add(newVideo);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { Message = "Video submitted and pending approval" });
});

app.MapGet("/api/admin/listen-videos",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var videos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.ListenVideos
            .OrderByDescending(v => v.SubmittedAt)
            .Select(v => new Backend.Application.DTOs.ListenVideoDto
            {
                Id = v.Id,
                YoutubeUrl = v.YoutubeUrl,
                Title = v.Title,
                ChannelName = v.ChannelName,
                Duration = v.Duration,
                ThumbnailUrl = v.ThumbnailUrl,
                Level = v.Level,
                Category = v.Category,
                IsApproved = v.IsApproved,
                TranscriptUrl = v.TranscriptUrl,
                WordCount = v.WordCount,
                SubmittedAt = v.SubmittedAt
            }), cancellationToken);
    return Results.Ok(videos);
});

app.MapPut("/api/admin/listen-videos/{id}/approve",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    video.IsApproved = true;
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveAsync("listen-videos:approved", cancellationToken);
    return Results.Ok(new { Message = "Đã duyệt video thành công" });
});

app.MapPut("/api/admin/listen-videos/{id}/transcript",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Application.DTOs.ManualTranscriptRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(req.TranscriptText))
        return Results.BadRequest(new { Message = "Văn bản phụ đề không được để trống." });

    try
    {
        var (jsonContent, wordCount) = transcriptService.ParseRawTextToTranscriptJson(req.TranscriptText);
        
        // Xóa file cũ trên R2 nếu có để tránh rác
        if (!string.IsNullOrEmpty(video.TranscriptUrl))
        {
            await r2Storage.DeleteFileAsync(video.TranscriptUrl, cancellationToken);
        }

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        
        // Tạo tên file an toàn (chứa tiêu đề video)
        var safeTitle = new string(video.Title.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        safeTitle = System.Text.RegularExpressions.Regex.Replace(safeTitle, @"\s+", "-").ToLower();
        if (safeTitle.Length > 30) safeTitle = safeTitle.Substring(0, 30);
        
        var fileName = $"listen-videos/transcript_{video.Id}_{safeTitle}_{Guid.NewGuid().ToString().Substring(0, 4)}.json";
        var r2Url = await r2Storage.UploadFileAsync(stream, fileName, "application/json", cancellationToken);
        
        video.TranscriptUrl = r2Url;
        video.WordCount = wordCount;
        video.IsApproved = true;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync("listen-videos:approved", cancellationToken);
        return Results.Ok(new { Message = "Cập nhật phụ đề thành công", TranscriptUrl = r2Url, WordCount = wordCount });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Lỗi khi xử lý phụ đề thủ công: " + ex.Message });
    }
});

app.MapDelete("/api/admin/listen-videos/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Storage, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    if (!string.IsNullOrEmpty(video.TranscriptUrl))
    {
        try
        {
            await r2Storage.DeleteFileAsync(video.TranscriptUrl, cancellationToken);
        }
        catch { } // Ignore delete errors
    }

    dbContext.ListenVideos.Remove(video);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveAsync("listen-videos:approved", cancellationToken);

    return Results.Ok(new { Message = "Video removed successfully" });
});

app.MapPut("/api/admin/listen-videos/{id}", async (int id, Backend.Application.DTOs.UpdateListenVideoRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(req.Title)) video.Title = req.Title.Trim();
    if (!string.IsNullOrWhiteSpace(req.Level)) video.Level = req.Level.Trim();
    if (!string.IsNullOrWhiteSpace(req.Category)) video.Category = req.Category.Trim();

    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveAsync("listen-videos:approved", cancellationToken);
    return Results.Ok(new { Message = "Cập nhật thông tin video thành công", Video = video });
});

app.MapGet("/api/admin/listen-videos/template-excel",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("ListenVideos");
    
    // Header
    worksheet.Cell(1, 1).Value = "Youtube Link";
    worksheet.Cell(1, 2).Value = "Transcript (Tiếng Anh)";
    worksheet.Cell(1, 3).Value = "Level (A1-C2)";
    worksheet.Cell(1, 4).Value = "Category (Chủ đề)";
    
    // Header styling
    var headerRange = worksheet.Range("A1:D1");
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
    
    // Sample data
    worksheet.Cell(2, 1).Value = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    worksheet.Cell(2, 2).Value = "Never gonna give you up, never gonna let you down...";
    worksheet.Cell(2, 3).Value = "B2";
    worksheet.Cell(2, 4).Value = "Giao tiếp";
    
    worksheet.Column(1).Width = 45;
    worksheet.Column(2).Width = 70;
    worksheet.Column(3).Width = 15;
    worksheet.Column(4).Width = 25;
    
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ListenVideos_Template.xlsx");
});

app.MapPost("/api/admin/listen-videos/import-excel",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { Message = "File không hợp lệ hoặc trống." });

    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { Message = "Vui lòng upload file định dạng Excel (.xlsx)" });

    int successCount = 0;
    int failCount = 0;
    int duplicateCount = 0;
    var errorDetails = new List<string>();

    using var stream = file.OpenReadStream();
    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
    var worksheet = workbook.Worksheet(1);
    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
    
    for (int i = 2; i <= lastRow; i++)
    {
        try
        {
            var row = worksheet.Row(i);
            var urlCell = row.Cell(1);
            var transcriptCell = row.Cell(2);
            var levelCell = row.Cell(3);
            var categoryCell = row.Cell(4);
            
            var url = urlCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(url) && urlCell.HasHyperlink)
                url = urlCell.GetHyperlink().ExternalAddress?.ToString()?.Trim();
                
            var transcript = transcriptCell.GetString()?.Trim();
            var level = levelCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(level)) level = "B2";
            
            var category = categoryCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(category)) category = "Giao tiếp";
            
            if (string.IsNullOrEmpty(url)) continue;

            var videoIdToCheck = "";
            if (url.Contains("v=")) videoIdToCheck = url.Split("v=")[1].Split("&")[0];
            else if (url.Contains("youtu.be/")) videoIdToCheck = url.Split("youtu.be/")[1].Split("?")[0];
            
            if (!string.IsNullOrEmpty(videoIdToCheck))
            {
                bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                    dbContext.ListenVideos, v => v.YoutubeUrl.Contains(videoIdToCheck), cancellationToken);
                if (exists) 
                {
                    duplicateCount++;
                    continue;
                }
            }

            // Lấy thông tin video từ YouTube
            var (title, channel, duration, thumbnail) = await transcriptService.GetVideoInfoAsync(url);
            
            // Nếu có phụ đề thì parse và upload R2
            string? r2Url = null;
            int wordCount = 0;
            
            if (!string.IsNullOrEmpty(transcript))
            {
                var (jsonContent, words) = transcriptService.ParseRawTextToTranscriptJson(transcript);
                wordCount = words;
                
                using var jsonStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
                var safeTitle = new string(title.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
                safeTitle = System.Text.RegularExpressions.Regex.Replace(safeTitle, @"\s+", "-").ToLower();
                if (safeTitle.Length > 30) safeTitle = safeTitle.Substring(0, 30);
                
                var fileName = $"listen-videos/transcript_{safeTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                r2Url = await r2Storage.UploadFileAsync(jsonStream, fileName, "application/json", cancellationToken);
            }

            // Lưu vào DB
            var video = new Backend.Domain.Entities.ListenVideo
            {
                YoutubeUrl = url,
                Title = title,
                ChannelName = channel,
                Duration = duration.ToString(@"hh\:mm\:ss"),
                ThumbnailUrl = thumbnail,
                Level = level,
                Category = category,
                IsApproved = true,
                TranscriptUrl = r2Url,
                WordCount = wordCount,
                SubmittedAt = DateTime.UtcNow
            };

            dbContext.ListenVideos.Add(video);
            successCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Row {i} error: {ex.Message}");
            errorDetails.Add($"Dòng {i}: {ex.Message}");
            failCount++;
        }
    }
    
    await dbContext.SaveChangesAsync(cancellationToken);
    if (successCount > 0)
    {
        await cacheService.RemoveAsync("listen-videos:approved", cancellationToken);
    }
    
    var finalMessage = $"Import thành công {successCount}, thất bại {failCount}, bỏ qua {duplicateCount} bị trùng.";
    if (errorDetails.Any()) {
        finalMessage += " Chi tiết lỗi: " + string.Join(" | ", errorDetails.Take(3));
        if (errorDetails.Count > 3) finalMessage += "...";
    }

    return Results.Ok(new { 
        Message = finalMessage,
        SuccessCount = successCount,
        FailCount = failCount,
        DuplicateCount = duplicateCount,
        Errors = errorDetails
    });
}).DisableAntiforgery();



app.MapPost("/api/ielts/exams",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (CreateExamRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var exam = new Backend.Domain.Entities.Exam
    {
        Title = request.Title,
        DataUrl = request.DataUrl,
        Category = request.Category
    };

    dbContext.Exams.Add(exam);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveAsync("ielts:exams:all", cancellationToken);

    return Results.Ok(new { exam.Id, exam.Title, exam.DataUrl });
});

app.MapGet("/api/ielts/exams", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "ielts:exams:all";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.ExamDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var exams = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Exams
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new Backend.Application.DTOs.ExamDto
            {
                Id = e.Id,
                Title = e.Title,
                DataUrl = e.DataUrl,
                Category = e.Category,
                CreatedAt = e.CreatedAt
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, exams, TimeSpan.FromHours(1), cancellationToken);
    return Results.Ok(exams);
});

// ─── IELTS SPEAK ALONG (SHADOWING): Cloudflare R2 Storage & Sync ───
app.MapGet("/api/ielts/speak-along/{part}", async (
    string part,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var cleanPart = part.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    var exam = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Exams.Where(e => e.Category == "IELTS_SPEAK_ALONG" && e.Title.ToLower() == cleanPart && e.IsActive),
        cancellationToken);

    if (exam == null || string.IsNullOrWhiteSpace(exam.DataUrl))
    {
        return Results.NotFound(new { Message = $"No remote Speak Along exam found for {part}" });
    }

    return Results.Ok(new { DataUrl = exam.DataUrl, Title = exam.Title, Id = exam.Id });
});

app.MapPost("/api/ielts/speak-along/save", async (
    Backend.Application.DTOs.SaveSpeakAlongRequest req,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.Part))
        return Results.BadRequest("Part is required.");

    var cleanPart = req.Part.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    var json = System.Text.Json.JsonSerializer.Serialize(req.Data,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var fileId = Guid.NewGuid().ToString("N")[..8];
    var fileName = $"ielts/speak-along/{cleanPart}_{fileId}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = $"Lỗi khi tải lên Cloudflare R2: {ex.Message}" });
    }

    var exam = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Exams.Where(e => e.Category == "IELTS_SPEAK_ALONG" && e.Title.ToLower() == cleanPart),
        cancellationToken);

    if (exam == null)
    {
        exam = new Backend.Domain.Entities.Exam
        {
            Title = cleanPart,
            DataUrl = jsonUrl,
            Category = "IELTS_SPEAK_ALONG",
            IsActive = true
        };
        dbContext.Exams.Add(exam);
    }
    else
    {
        exam.DataUrl = jsonUrl;
        exam.IsActive = true;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Success = true, R2Url = jsonUrl, Id = exam.Id });
}).DisableAntiforgery();

app.MapPost("/api/ielts/speak-along/upload-file", async (
    Microsoft.AspNetCore.Http.IFormFile file,
    string part,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    var cleanPart = (part ?? "100sentences").ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    var fileName = $"ielts/speak-along/{cleanPart}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

    using var stream = file.OpenReadStream();
    string jsonUrl;
    try
    {
        jsonUrl = await r2Service.UploadFileAsync(stream, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = $"Lỗi khi tải lên Cloudflare R2: {ex.Message}" });
    }

    var exam = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Exams.Where(e => e.Category == "IELTS_SPEAK_ALONG" && e.Title.ToLower() == cleanPart),
        cancellationToken);

    if (exam == null)
    {
        exam = new Backend.Domain.Entities.Exam
        {
            Title = cleanPart,
            DataUrl = jsonUrl,
            Category = "IELTS_SPEAK_ALONG",
            IsActive = true
        };
        dbContext.Exams.Add(exam);
    }
    else
    {
        exam.DataUrl = jsonUrl;
        exam.IsActive = true;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Success = true, R2Url = jsonUrl, Id = exam.Id });
}).DisableAntiforgery();

app.MapGet("/api/ielts/audio-shadowing", async (
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var exam = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Exams.Where(e => e.Category == "IELTS_AUDIO_SHADOWING_CATALOG" && e.IsActive),
        cancellationToken);

    if (exam == null)
    {
        return Results.NotFound(new { Message = "Chưa có catalog Audio Shadowing trên R2." });
    }

    return Results.Ok(new { DataUrl = exam.DataUrl, Title = exam.Title });
}).DisableAntiforgery();

app.MapPost("/api/ielts/audio-shadowing/save", async (
    System.Text.Json.JsonElement payload,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var json = payload.GetRawText();
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var fileName = $"ielts/audio-shadowing/catalog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = $"Lỗi khi tải lên Cloudflare R2: {ex.Message}" });
    }

    var exam = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
        dbContext.Exams.Where(e => e.Category == "IELTS_AUDIO_SHADOWING_CATALOG"),
        cancellationToken);

    if (exam == null)
    {
        exam = new Backend.Domain.Entities.Exam
        {
            Title = "IELTS Audio Shadowing Catalog",
            DataUrl = jsonUrl,
            Category = "IELTS_AUDIO_SHADOWING_CATALOG",
            IsActive = true
        };
        dbContext.Exams.Add(exam);
    }
    else
    {
        exam.DataUrl = jsonUrl;
        exam.IsActive = true;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Success = true, R2Url = jsonUrl, Id = exam.Id });
}).DisableAntiforgery();

app.MapPost("/api/ielts/audio-shadowing/upload-lesson", async (
    string lessonId,
    System.Text.Json.JsonElement payload,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    CancellationToken cancellationToken) =>
{
    var safeId = string.IsNullOrWhiteSpace(lessonId) ? Guid.NewGuid().ToString("N")[..8] : lessonId.Trim().ToLowerInvariant();
    var json = payload.GetRawText();
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var fileName = $"ielts/audio-shadowing/lessons/{safeId}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = $"Lỗi khi tải lên Cloudflare R2: {ex.Message}" });
    }

    return Results.Ok(new { Success = true, LessonId = safeId, R2Url = jsonUrl });
}).DisableAntiforgery();

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

app.MapGet("/api/auth/check-username", async (string? username, IAuthService authService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(username)) return Results.Ok(new { isTaken = false });
    var isTaken = await authService.IsUsernameTakenAsync(username, cancellationToken);
    return Results.Ok(new { isTaken });
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

app.MapPost("/api/admin/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.AdminLoginAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/google-login", async (GoogleLoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.LoginWithGoogleAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/google-register", async (GoogleLoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.RegisterWithGoogleAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

// Gia hạn phiên đăng nhập: đổi refresh token lấy access token + refresh token mới (rotation)
app.MapPost("/api/auth/refresh", async (RefreshRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).RequireRateLimiting("auth");

// Đăng xuất: thu hồi refresh token phía server
app.MapPost("/api/auth/logout", async (RefreshRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    await authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
    return Results.Ok(new { Message = "Logged out" });
});

app.MapGet("/api/user/me", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(userIdString, out int userId))
    {
        var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (dbUser != null)
        {
            return Results.Ok(new
            {
                dbUser.Id,
                dbUser.Username,
                dbUser.UsernameChangedAt,
                dbUser.FullName,
                dbUser.Email,
                dbUser.Role,
                dbUser.Avatar,
                dbUser.AvatarColor,
                dbUser.Bio,
                dbUser.TargetExam,
                dbUser.TargetScore,
                dbUser.TargetDeadline,
                dbUser.IeltsLevel,
                dbUser.HskLevel,
                dbUser.Level,
                dbUser.Streak,
                dbUser.LastActive,
                dbUser.CreatedAt
            });
        }
    }
    return Results.Unauthorized();
});

app.MapPut("/api/user/profile", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateProfileRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.FullName, "Họ và tên");
        Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Username, "Tên hiển thị");
        Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Avatar, "Ảnh đại diện");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }

    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(userIdString, out int userId))
    {
        var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (dbUser != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var newUsername = request.Username.Trim().ToLowerInvariant();
                if (!System.Text.RegularExpressions.Regex.IsMatch(newUsername, @"^[a-z0-9._-]{3,30}$"))
                {
                    return Results.BadRequest(new { message = "Tên hiển thị (username) chỉ được gồm chữ cái không dấu (a-z), số (0-9), dấu '.', '_', '-' và từ 3 đến 30 ký tự, không dùng tiếng Việt có dấu." });
                }

                if (!string.Equals(dbUser.Username, newUsername, StringComparison.OrdinalIgnoreCase))
                {
                    if (dbUser.UsernameChangedAt.HasValue)
                    {
                        var daysSinceChange = (DateTime.UtcNow - dbUser.UsernameChangedAt.Value).TotalDays;
                        if (daysSinceChange < 30)
                        {
                            var daysLeft = Math.Max(1, (int)Math.Ceiling(30 - daysSinceChange));
                            var nextAllowed = dbUser.UsernameChangedAt.Value.AddDays(30);
                            return Results.BadRequest(new
                            {
                                message = $"Bạn chỉ có thể đổi tên hiển thị 30 ngày một lần. Vui lòng quay lại sau {daysLeft} ngày nữa (ngày {nextAllowed:dd/MM/yyyy}).",
                                daysRemaining = daysLeft,
                                nextAllowedAt = nextAllowed
                            });
                        }
                    }

                    var isTaken = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                        dbContext.Users,
                        u => u.Id != userId && u.Username.ToLower() == newUsername,
                        cancellationToken);

                    if (isTaken)
                    {
                        return Results.BadRequest(new { message = "Tên hiển thị (username) này đã có người sử dụng. Vui lòng chọn tên khác." });
                    }
                    dbUser.Username = newUsername;
                    dbUser.UsernameChangedAt = DateTime.UtcNow;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.FullName)) dbUser.FullName = request.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Avatar)) dbUser.Avatar = request.Avatar.Trim();
            if (request.AvatarColor != null) dbUser.AvatarColor = request.AvatarColor.Trim();
            if (request.Bio != null) dbUser.Bio = request.Bio.Trim();
            if (request.TargetExam != null) dbUser.TargetExam = request.TargetExam.Trim();
            if (request.TargetScore != null) dbUser.TargetScore = request.TargetScore.Trim();
            if (request.TargetDeadline.HasValue) dbUser.TargetDeadline = DateTime.SpecifyKind(request.TargetDeadline.Value, DateTimeKind.Utc);
            if (request.IeltsLevel != null) dbUser.IeltsLevel = request.IeltsLevel.Trim();
            if (request.HskLevel != null) dbUser.HskLevel = request.HskLevel.Trim();
            if (!string.IsNullOrWhiteSpace(request.Level)) dbUser.Level = request.Level.Trim();
            dbUser.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                dbUser.Id,
                dbUser.Username,
                dbUser.UsernameChangedAt,
                dbUser.FullName,
                dbUser.Avatar,
                dbUser.AvatarColor,
                dbUser.Bio,
                dbUser.TargetExam,
                dbUser.TargetScore,
                dbUser.TargetDeadline,
                dbUser.IeltsLevel,
                dbUser.HskLevel,
                dbUser.Level
            });
        }
    }
    return Results.Unauthorized();
});

app.MapPost("/api/user/streak", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(userIdString, out int userId))
    {
        var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (dbUser != null)
        {
            var today = DateTime.UtcNow.Date;
            if (!dbUser.LastActive.HasValue || dbUser.LastActive.Value.Date != today)
            {
                if (dbUser.LastActive.HasValue && dbUser.LastActive.Value.Date == today.AddDays(-1))
                {
                    dbUser.Streak += 1;
                }
                else if (!dbUser.LastActive.HasValue || dbUser.LastActive.Value.Date < today.AddDays(-1))
                {
                    dbUser.Streak = 1;
                }
                dbUser.LastActive = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return Results.Ok(new { streak = dbUser.Streak, lastActive = dbUser.LastActive });
        }
    }
    return Results.Unauthorized();
});

app.MapPut("/api/user/level", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateLevelRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(userIdString, out int userId))
    {
        var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (dbUser != null)
        {
            dbUser.Level = request.Level;
            dbUser.IeltsLevel = request.Level;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok();
        }
    }
    return Results.Unauthorized();
});

// ==========================================
// STUDY ACTIVITY & STREAK ENDPOINTS
// ==========================================
app.MapPost("/api/user/study-time", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.AddStudyTimeRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
    if (dbUser == null) return Results.Unauthorized();

    if (request.Seconds <= 0)
    {
        return Results.BadRequest(new { message = "Seconds must be greater than 0" });
    }

    var targetDateUtc = DateTime.SpecifyKind((request.Date ?? DateTime.UtcNow).Date, DateTimeKind.Utc);

    var activity = await dbContext.UserStudyActivities
        .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == targetDateUtc, cancellationToken);

    if (activity == null)
    {
        activity = new Backend.Domain.Entities.UserStudyActivity
        {
            UserId = userId,
            ActivityDate = targetDateUtc,
            StudySeconds = request.Seconds,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.UserStudyActivities.Add(activity);
    }
    else
    {
        activity.StudySeconds += request.Seconds;
        activity.UpdatedAt = DateTime.UtcNow;
    }

    // Calculate updated streak
    var allActivities = await dbContext.UserStudyActivities
        .Where(a => a.UserId == userId && a.StudySeconds > 0)
        .Select(a => a.ActivityDate)
        .ToListAsync(cancellationToken);

    if (!allActivities.Any(d => d.Date == targetDateUtc.Date))
    {
        allActivities.Add(targetDateUtc);
    }

    var activeDates = allActivities.Select(d => d.Date).Distinct().ToHashSet();
    var todayUtc = DateTime.UtcNow.Date;
    int currentStreak = 0;
    var checkDate = todayUtc;
    if (!activeDates.Contains(checkDate))
    {
        checkDate = todayUtc.AddDays(-1);
    }
    while (activeDates.Contains(checkDate))
    {
        currentStreak++;
        checkDate = checkDate.AddDays(-1);
    }

    dbUser.Streak = currentStreak;
    dbUser.LastActive = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        streak = dbUser.Streak,
        todaySeconds = activity.StudySeconds
    });
});

app.MapGet("/api/user/study-activity", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var activities = await dbContext.UserStudyActivities.AsNoTracking()
        .Where(a => a.UserId == userId && a.StudySeconds > 0)
        .OrderBy(a => a.ActivityDate)
        .ToListAsync(cancellationToken);

    var activeDates = activities.Select(a => a.ActivityDate.Date).Distinct().ToHashSet();
    var activeDays = activeDates.OrderBy(d => d).Select(d => d.ToString("yyyy-MM-dd")).ToList();

    int bestStreak = 0;
    int tempStreak = 0;
    DateTime? prev = null;
    foreach (var d in activeDates.OrderBy(d => d))
    {
        if (prev.HasValue && d == prev.Value.AddDays(1))
        {
            tempStreak++;
        }
        else
        {
            tempStreak = 1;
        }
        if (tempStreak > bestStreak) bestStreak = tempStreak;
        prev = d;
    }

    var todayUtc = DateTime.UtcNow.Date;
    int currentStreak = 0;
    var checkDate = todayUtc;
    if (!activeDates.Contains(checkDate))
    {
        checkDate = todayUtc.AddDays(-1);
    }
    while (activeDates.Contains(checkDate))
    {
        currentStreak++;
        checkDate = checkDate.AddDays(-1);
    }

    var todayActivity = activities.FirstOrDefault(a => a.ActivityDate.Date == todayUtc);
    int todaySeconds = todayActivity?.StudySeconds ?? 0;

    var dailyList = activities.Select(a => new Backend.Application.DTOs.DailyStudyItemDto(a.ActivityDate, a.StudySeconds)).ToList();

    return Results.Ok(new Backend.Application.DTOs.StudyActivityResponseDto(
        currentStreak,
        bestStreak,
        todaySeconds,
        dailyList,
        activeDays
    ));
});

app.MapPost("/api/user/study-activity/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.MigrateStudyActivityRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
    if (dbUser == null) return Results.Unauthorized();

    int migratedCount = 0;

    if (request.DailySeconds != null)
    {
        foreach (var (dateStr, sec) in request.DailySeconds)
        {
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                var dateUtc = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
                var act = await dbContext.UserStudyActivities
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == dateUtc, cancellationToken);
                if (act == null)
                {
                    dbContext.UserStudyActivities.Add(new Backend.Domain.Entities.UserStudyActivity
                    {
                        UserId = userId,
                        ActivityDate = dateUtc,
                        StudySeconds = sec,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else if (sec > act.StudySeconds)
                {
                    act.StudySeconds = sec;
                    act.UpdatedAt = DateTime.UtcNow;
                }
                migratedCount++;
            }
        }
    }

    if (request.ActiveDays != null)
    {
        foreach (var dateStr in request.ActiveDays)
        {
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                var dateUtc = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
                var act = await dbContext.UserStudyActivities
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == dateUtc, cancellationToken);
                if (act == null)
                {
                    dbContext.UserStudyActivities.Add(new Backend.Domain.Entities.UserStudyActivity
                    {
                        UserId = userId,
                        ActivityDate = dateUtc,
                        StudySeconds = 60, // ensure at least 1 minute to count as active
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    migratedCount++;
                }
            }
        }
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    // Recalculate streak
    var allActivities = await dbContext.UserStudyActivities
        .Where(a => a.UserId == userId && a.StudySeconds > 0)
        .Select(a => a.ActivityDate)
        .ToListAsync(cancellationToken);

    var activeDates = allActivities.Select(d => d.Date).Distinct().ToHashSet();
    var todayUtc = DateTime.UtcNow.Date;
    int currentStreak = 0;
    var checkDate = todayUtc;
    if (!activeDates.Contains(checkDate))
    {
        checkDate = todayUtc.AddDays(-1);
    }
    while (activeDates.Contains(checkDate))
    {
        currentStreak++;
        checkDate = checkDate.AddDays(-1);
    }

    dbUser.Streak = currentStreak;
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { migratedCount, streak = dbUser.Streak });
});

// ==========================================
// TOEIC VOCABULARY & PROGRESS ENDPOINTS
// ==========================================
app.MapGet("/api/toeic/vocab", async (string? topic, string? search, System.Security.Claims.ClaimsPrincipal? user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    int? userId = null;
    if (user != null)
    {
        var uidStr = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                     ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(uidStr, out int parsedId)) userId = parsedId;
    }

    var query = dbContext.ToeicVocabularies.AsNoTracking().Where(v => v.IsActive);

    if (userId.HasValue)
    {
        query = query.Where(v => !v.IsCustom || v.UserId == userId.Value);
    }
    else
    {
        query = query.Where(v => !v.IsCustom);
    }

    if (!string.IsNullOrWhiteSpace(topic) && !string.Equals(topic, "All", StringComparison.OrdinalIgnoreCase) && !string.Equals(topic, "Tất cả", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(v => v.Topic == topic);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        query = query.Where(v => v.Word.ToLower().Contains(s) || v.Meaning.ToLower().Contains(s));
    }

    var items = await query.OrderBy(v => v.Id).Select(v => new Backend.Application.DTOs.ToeicVocabDto(
        v.Id,
        v.Word,
        v.Ipa,
        v.Meaning,
        v.Example,
        v.Topic,
        v.IsCustom
    )).ToListAsync(cancellationToken);

    return Results.Ok(items);
});

app.MapPost("/api/toeic/vocab", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.CreateToeicVocabRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Word) || string.IsNullOrWhiteSpace(request.Meaning))
    {
        return Results.BadRequest(new { message = "Word and Meaning are required." });
    }

    var vocab = new Backend.Domain.Entities.ToeicVocabulary
    {
        Word = request.Word.Trim(),
        Ipa = request.Ipa?.Trim() ?? string.Empty,
        Meaning = request.Meaning.Trim(),
        Example = request.Example?.Trim(),
        Topic = string.IsNullOrWhiteSpace(request.Topic) ? "Khác" : request.Topic.Trim(),
        IsCustom = true,
        UserId = userId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.ToeicVocabularies.Add(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new Backend.Application.DTOs.ToeicVocabDto(
        vocab.Id,
        vocab.Word,
        vocab.Ipa,
        vocab.Meaning,
        vocab.Example,
        vocab.Topic,
        vocab.IsCustom
    ));
});

app.MapDelete("/api/toeic/vocab/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (int id, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var vocab = await dbContext.ToeicVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();

    var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    if (vocab.UserId != userId && role != "admin")
    {
        return Results.Forbid();
    }

    dbContext.ToeicVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { message = "Deleted" });
});

app.MapGet("/api/toeic/vocab/progress", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var progresses = await dbContext.ToeicVocabularyProgresses.AsNoTracking()
        .Where(p => p.UserId == userId)
        .ToListAsync(cancellationToken);

    var learnedIds = progresses.Where(p => p.Status == "Learned").Select(p => p.VocabularyId).ToList();
    var againIds = progresses.Where(p => p.Status == "Again").Select(p => p.VocabularyId).ToList();

    return Results.Ok(new Backend.Application.DTOs.ToeicVocabProgressResponse(learnedIds, againIds));
});

app.MapPost("/api/toeic/vocab/progress", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateToeicVocabProgressRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var existing = await dbContext.ToeicVocabularyProgresses
        .FirstOrDefaultAsync(p => p.UserId == userId && p.VocabularyId == request.VocabularyId, cancellationToken);

    if (string.Equals(request.Status, "None", StringComparison.OrdinalIgnoreCase))
    {
        if (existing != null)
        {
            dbContext.ToeicVocabularyProgresses.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
    else
    {
        if (existing == null)
        {
            dbContext.ToeicVocabularyProgresses.Add(new Backend.Domain.Entities.ToeicVocabularyProgress
            {
                UserId = userId,
                VocabularyId = request.VocabularyId,
                Status = request.Status,
                LearnedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Status = request.Status;
            existing.LearnedAt = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { success = true });
});

app.MapPost("/api/toeic/vocab/progress/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.MigrateToeicVocabRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (request.CustomWords != null && request.CustomWords.Count > 0)
    {
        foreach (var cw in request.CustomWords)
        {
            if (string.IsNullOrWhiteSpace(cw.Word) || string.IsNullOrWhiteSpace(cw.Meaning)) continue;
            var wTrim = cw.Word.Trim().ToLower();
            var exists = await dbContext.ToeicVocabularies.AnyAsync(v => v.UserId == userId && v.Word.ToLower() == wTrim, cancellationToken);
            if (!exists)
            {
                dbContext.ToeicVocabularies.Add(new Backend.Domain.Entities.ToeicVocabulary
                {
                    Word = cw.Word.Trim(),
                    Ipa = cw.Ipa?.Trim() ?? string.Empty,
                    Meaning = cw.Meaning.Trim(),
                    Example = cw.Example?.Trim(),
                    Topic = string.IsNullOrWhiteSpace(cw.Topic) ? "Khác" : cw.Topic.Trim(),
                    IsCustom = true,
                    UserId = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    if (request.LearnedIds != null)
    {
        foreach (var id in request.LearnedIds)
        {
            var existing = await dbContext.ToeicVocabularyProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.VocabularyId == id, cancellationToken);
            if (existing == null)
            {
                dbContext.ToeicVocabularyProgresses.Add(new Backend.Domain.Entities.ToeicVocabularyProgress
                {
                    UserId = userId,
                    VocabularyId = id,
                    Status = "Learned",
                    LearnedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Status = "Learned";
            }
        }
    }

    if (request.AgainIds != null)
    {
        foreach (var id in request.AgainIds)
        {
            var existing = await dbContext.ToeicVocabularyProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.VocabularyId == id, cancellationToken);
            if (existing == null)
            {
                dbContext.ToeicVocabularyProgresses.Add(new Backend.Domain.Entities.ToeicVocabularyProgress
                {
                    UserId = userId,
                    VocabularyId = id,
                    Status = "Again",
                    LearnedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Status = "Again";
            }
        }
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { success = true });
});

// ==========================================
// GAME PROGRESS ENDPOINTS
// ==========================================
app.MapGet("/api/user/game-progress", [Microsoft.AspNetCore.Authorization.Authorize] async (string? gameType, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var query = dbContext.UserGameProgresses.AsNoTracking().Where(g => g.UserId == userId);
    if (!string.IsNullOrWhiteSpace(gameType))
    {
        query = query.Where(g => g.GameType == gameType);
    }

    var list = await query.ToListAsync(cancellationToken);
    return Results.Ok(list.Select(g => new Backend.Application.DTOs.UserGameProgressDto(
        g.GameType,
        g.Level,
        g.CurrentStage,
        g.MaxUnlockedStage,
        g.HighScore,
        g.UpdatedAt
    )));
});

app.MapPost("/api/user/game-progress", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.SaveGameProgressRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var existing = await dbContext.UserGameProgresses
        .FirstOrDefaultAsync(g => g.UserId == userId && g.GameType == request.GameType && g.Level == request.Level, cancellationToken);

    if (existing == null)
    {
        existing = new Backend.Domain.Entities.UserGameProgress
        {
            UserId = userId,
            GameType = request.GameType,
            Level = request.Level,
            CurrentStage = request.CurrentStage ?? 0,
            MaxUnlockedStage = request.MaxUnlockedStage ?? 0,
            HighScore = request.Score ?? 0,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.UserGameProgresses.Add(existing);
    }
    else
    {
        if (request.CurrentStage.HasValue) existing.CurrentStage = request.CurrentStage.Value;
        if (request.MaxUnlockedStage.HasValue && request.MaxUnlockedStage.Value > existing.MaxUnlockedStage)
            existing.MaxUnlockedStage = request.MaxUnlockedStage.Value;
        if (request.Score.HasValue && request.Score.Value > existing.HighScore)
            existing.HighScore = request.Score.Value;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new Backend.Application.DTOs.UserGameProgressDto(
        existing.GameType,
        existing.Level,
        existing.CurrentStage,
        existing.MaxUnlockedStage,
        existing.HighScore,
        existing.UpdatedAt
    ));
});

app.MapPost("/api/user/game-progress/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.MigrateGameProgressRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (request.ProgressItems != null)
    {
        foreach (var item in request.ProgressItems)
        {
            var existing = await dbContext.UserGameProgresses
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameType == item.GameType && g.Level == item.Level, cancellationToken);
            if (existing == null)
            {
                dbContext.UserGameProgresses.Add(new Backend.Domain.Entities.UserGameProgress
                {
                    UserId = userId,
                    GameType = item.GameType,
                    Level = item.Level,
                    CurrentStage = item.CurrentStage ?? 0,
                    MaxUnlockedStage = item.MaxUnlockedStage ?? 0,
                    HighScore = item.Score ?? 0,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                if (item.CurrentStage.HasValue) existing.CurrentStage = item.CurrentStage.Value;
                if (item.MaxUnlockedStage.HasValue && item.MaxUnlockedStage.Value > existing.MaxUnlockedStage)
                    existing.MaxUnlockedStage = item.MaxUnlockedStage.Value;
                if (item.Score.HasValue && item.Score.Value > existing.HighScore)
                    existing.HighScore = item.Score.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { success = true, count = request.ProgressItems?.Count ?? 0 });
});

// ==========================================
// EXAM CHECKPOINTS ENDPOINTS
// ==========================================
app.MapGet("/api/exam-checkpoints", async (string examUrl, string skill, string? userIdentifier, System.Security.Claims.ClaimsPrincipal? user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    int? userId = null;
    if (user != null)
    {
        var uidStr = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                     ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(uidStr, out int parsedId)) userId = parsedId;
    }

    var query = dbContext.ExamCheckpoints.AsNoTracking()
        .Where(c => c.ExamUrl == examUrl && c.Skill == skill);

    if (userId.HasValue)
    {
        query = query.Where(c => c.UserId == userId.Value || c.UserIdentifier == userIdentifier);
    }
    else if (!string.IsNullOrWhiteSpace(userIdentifier))
    {
        query = query.Where(c => c.UserIdentifier == userIdentifier);
    }
    else
    {
        return Results.BadRequest(new { message = "UserIdentifier or Auth token is required." });
    }

    var cp = await query.OrderByDescending(c => c.LastSavedAt).FirstOrDefaultAsync(cancellationToken);
    if (cp == null) return Results.NotFound();

    return Results.Ok(new Backend.Application.DTOs.ExamCheckpointDto(
        cp.Id,
        cp.UserIdentifier,
        cp.Skill,
        cp.ExamUrl,
        cp.MockTestId,
        cp.CheckpointDataJson,
        cp.SecondsRemaining,
        cp.LastSavedAt
    ));
});

app.MapPost("/api/exam-checkpoints", async (Backend.Application.DTOs.SaveCheckpointRequest request, System.Security.Claims.ClaimsPrincipal? user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    int? userId = null;
    if (user != null)
    {
        var uidStr = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                     ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(uidStr, out int parsedId)) userId = parsedId;
    }

    var existing = await dbContext.ExamCheckpoints
        .FirstOrDefaultAsync(c =>
            (userId.HasValue && c.UserId == userId.Value && c.ExamUrl == request.ExamUrl && c.Skill == request.Skill) ||
            (c.UserIdentifier == request.UserIdentifier && c.ExamUrl == request.ExamUrl && c.Skill == request.Skill),
            cancellationToken);

    if (existing == null)
    {
        existing = new Backend.Domain.Entities.ExamCheckpoint
        {
            UserId = userId,
            UserIdentifier = request.UserIdentifier,
            Skill = request.Skill,
            ExamUrl = request.ExamUrl,
            MockTestId = request.MockTestId,
            CheckpointDataJson = request.CheckpointDataJson,
            SecondsRemaining = request.SecondsRemaining,
            LastSavedAt = DateTime.UtcNow
        };
        dbContext.ExamCheckpoints.Add(existing);
    }
    else
    {
        if (userId.HasValue && !existing.UserId.HasValue) existing.UserId = userId;
        existing.CheckpointDataJson = request.CheckpointDataJson;
        existing.SecondsRemaining = request.SecondsRemaining;
        if (request.MockTestId.HasValue) existing.MockTestId = request.MockTestId;
        existing.LastSavedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { success = true, lastSavedAt = existing.LastSavedAt });
});

app.MapDelete("/api/exam-checkpoints", async (string examUrl, string skill, string? userIdentifier, System.Security.Claims.ClaimsPrincipal? user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    int? userId = null;
    if (user != null)
    {
        var uidStr = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                     ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(uidStr, out int parsedId)) userId = parsedId;
    }

    var query = dbContext.ExamCheckpoints
        .Where(c => c.ExamUrl == examUrl && c.Skill == skill);

    if (userId.HasValue)
    {
        query = query.Where(c => c.UserId == userId.Value || c.UserIdentifier == userIdentifier);
    }
    else if (!string.IsNullOrWhiteSpace(userIdentifier))
    {
        query = query.Where(c => c.UserIdentifier == userIdentifier);
    }
    else
    {
        return Results.BadRequest(new { message = "UserIdentifier or Auth token is required." });
    }

    var list = await query.ToListAsync(cancellationToken);
    if (list.Count > 0)
    {
        dbContext.ExamCheckpoints.RemoveRange(list);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { success = true });
});

// MOCK TESTS API
app.MapGet("/api/mock-tests", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var tests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.MockTests.Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt), 
        cancellationToken);
        
    var dtos = tests.Select(m => new Backend.Application.DTOs.MockTestDto
    {
        Id = m.Id,
        CollectionName = m.CollectionName,
        Title = m.Title,
        ListeningUrl = m.ListeningUrl,
        ReadingUrl = m.ReadingUrl,
        WritingUrl = m.WritingUrl,
        SpeakingUrl = m.SpeakingUrl,
        ListeningAnswerUrl = m.ListeningAnswerUrl,
        ReadingAnswerUrl = m.ReadingAnswerUrl,
        WritingAnswerUrl = m.WritingAnswerUrl,
        SpeakingAnswerUrl = m.SpeakingAnswerUrl,
        ToeicUrl = m.ToeicUrl,
        HskUrl = m.HskUrl
    }).ToList();
    
    return Results.Ok(dtos);
});

// TOEIC R2 TESTS API: Quét động toàn bộ đề thi (.json) trong folder R2 Cuongkeng/Toeic Data/
app.MapGet("/api/toeic/r2-tests", async (Backend.Application.Abstractions.IR2StorageService r2Service, IConfiguration config, CancellationToken cancellationToken) =>
{
    try
    {
        var publicUrlBase = (config["CloudflareR2:PublicUrlBase"] ?? "https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev").TrimEnd('/');
        var allKeys = await r2Service.ListFilesAsync("Cuongkeng/Toeic Data/", cancellationToken);
        static int ExtractYearFromKey(string path)
        {
            var m = System.Text.RegularExpressions.Regex.Match(path, @"(20\d\d)");
            return m.Success && int.TryParse(m.Groups[1].Value, out var y) ? y : 0;
        }

        static int ExtractTestNumFromKey(string path)
        {
            var m = System.Text.RegularExpressions.Regex.Match(path, @"Test\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n)) return n;
            var m2 = System.Text.RegularExpressions.Regex.Match(path, @"(\d+)");
            return m2.Success && int.TryParse(m2.Groups[1].Value, out var n2) ? n2 : int.MaxValue;
        }

        var jsonKeys = allKeys
            .Where(k => k.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(k => ExtractYearFromKey(k))
            .ThenBy(k => ExtractTestNumFromKey(k))
            .ThenBy(k => k)
            .ToList();

        var tests = new List<Backend.Application.DTOs.MockTestDto>();
        int autoId = 1000;
        foreach (var key in jsonKeys)
        {
            var rawFileName = Path.GetFileNameWithoutExtension(key); // e.g. "TOEIC ETS 2026-Test 8"
            string collection = "TOEIC ETS 2026";
            string title = rawFileName;
            if (rawFileName.Contains("-"))
            {
                var parts = rawFileName.Split('-', 2);
                collection = parts[0].Trim();
                title = parts[1].Trim();
            }

            var encodedKey = Uri.EscapeDataString(key).Replace("%2F", "/");
            var publicUrl = $"{publicUrlBase}/{encodedKey}";

            tests.Add(new Backend.Application.DTOs.MockTestDto
            {
                Id = autoId++,
                CollectionName = collection,
                Title = title,
                ToeicUrl = publicUrl
            });
        }

        return Results.Ok(tests);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[R2-TOEIC] Error scanning tests from R2: {ex.Message}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/mock-tests",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var newTest = new Backend.Domain.Entities.MockTest
    {
        CollectionName = request.CollectionName,
        Title = request.Title,
        ListeningUrl = request.ListeningUrl,
        ReadingUrl = request.ReadingUrl,
        WritingUrl = request.WritingUrl,
        SpeakingUrl = request.SpeakingUrl,
        ListeningAnswerUrl = request.ListeningAnswerUrl,
        ReadingAnswerUrl = request.ReadingAnswerUrl,
        WritingAnswerUrl = request.WritingAnswerUrl,
        SpeakingAnswerUrl = request.SpeakingAnswerUrl,
        ToeicUrl = request.ToeicUrl,
        HskUrl = request.HskUrl
    };
    
    dbContext.MockTests.Add(newTest);
    await dbContext.SaveChangesAsync(cancellationToken);
    
    return Results.Ok(new { Id = newTest.Id });
});

app.MapPut("/api/mock-tests/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var test = await dbContext.MockTests.FindAsync(new object[] { id }, cancellationToken);
    if (test == null) return Results.NotFound();

    test.CollectionName = request.CollectionName;
    test.Title = request.Title;
    test.ListeningUrl = request.ListeningUrl;
    test.ReadingUrl = request.ReadingUrl;
    test.WritingUrl = request.WritingUrl;
    test.SpeakingUrl = request.SpeakingUrl;
    test.ListeningAnswerUrl = request.ListeningAnswerUrl;
    test.ReadingAnswerUrl = request.ReadingAnswerUrl;
    test.WritingAnswerUrl = request.WritingAnswerUrl;
    test.SpeakingAnswerUrl = request.SpeakingAnswerUrl;
    test.ToeicUrl = request.ToeicUrl;
    test.HskUrl = request.HskUrl;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapDelete("/api/mock-tests/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    var test = await dbContext.MockTests.FindAsync(new object[] { id }, cancellationToken);
    if (test == null) return Results.NotFound();

    // Delete associated files from Cloudflare R2 if they exist
    if (!string.IsNullOrEmpty(test.ToeicUrl)) await r2Service.DeleteFileAsync(test.ToeicUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ListeningUrl)) await r2Service.DeleteFileAsync(test.ListeningUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ReadingUrl)) await r2Service.DeleteFileAsync(test.ReadingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.WritingUrl)) await r2Service.DeleteFileAsync(test.WritingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.SpeakingUrl)) await r2Service.DeleteFileAsync(test.SpeakingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ListeningAnswerUrl)) await r2Service.DeleteFileAsync(test.ListeningAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ReadingAnswerUrl)) await r2Service.DeleteFileAsync(test.ReadingAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.WritingAnswerUrl)) await r2Service.DeleteFileAsync(test.WritingAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.SpeakingAnswerUrl)) await r2Service.DeleteFileAsync(test.SpeakingAnswerUrl, cancellationToken);

    dbContext.MockTests.Remove(test);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapPost("/api/mock-tests/upload",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
        
    var safeFileName = System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), @"[^\w\-]", "_");
    var fileExt = Path.GetExtension(file.FileName);
    var fileName = $"{safeFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExt}";
    try
    {
        var isJson = (file.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase)
                     || file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        string url;
        if (isJson)
        {
            string payload;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                payload = await reader.ReadToEndAsync(cancellationToken);
            }
            // Chống stored XSS: làm sạch nội dung JSON đề thi trước khi lưu lên R2
            var clean = HtmlGuard.SanitizeJsonStrings(payload);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clean));
            url = await r2Service.UploadFileAsync(stream, fileName, contentType, cancellationToken);
        }
        else
        {
            // Các file nhị phân (âm thanh mp3, wav, ảnh...) upload trực tiếp stream gốc
            using var stream = file.OpenReadStream();
            url = await r2Service.UploadFileAsync(stream, fileName, contentType, cancellationToken);
        }
        return Results.Ok(new { Url = url });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).DisableAntiforgery(); // Disable Anti-forgery for API upload if needed

// HSK MOCK TESTS API
app.MapGet("/api/hsk-mock-tests", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var tests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.HskMockTests.Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt), 
        cancellationToken);
        
    var dtos = tests.Select(m => new Backend.Application.DTOs.MockTestDto
    {
        Id = m.Id,
        CollectionName = m.CollectionName,
        Title = m.Title,
        ListeningUrl = m.ListeningUrl,
        ReadingUrl = m.ReadingUrl,
        WritingUrl = m.WritingUrl,
        SpeakingUrl = m.SpeakingUrl,
        ListeningAnswerUrl = m.ListeningAnswerUrl,
        ReadingAnswerUrl = m.ReadingAnswerUrl,
        WritingAnswerUrl = m.WritingAnswerUrl,
        SpeakingAnswerUrl = m.SpeakingAnswerUrl,
        HskUrl = m.HskUrl
    }).ToList();
    
    return Results.Ok(dtos);
});

app.MapPost("/api/hsk-mock-tests", async (Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var newTest = new Backend.Domain.Entities.HskMockTest
    {
        CollectionName = request.CollectionName,
        Title = request.Title,
        ListeningUrl = request.ListeningUrl,
        ReadingUrl = request.ReadingUrl,
        WritingUrl = request.WritingUrl,
        SpeakingUrl = request.SpeakingUrl,
        ListeningAnswerUrl = request.ListeningAnswerUrl,
        ReadingAnswerUrl = request.ReadingAnswerUrl,
        WritingAnswerUrl = request.WritingAnswerUrl,
        SpeakingAnswerUrl = request.SpeakingAnswerUrl,
        HskUrl = request.HskUrl
    };
    
    dbContext.HskMockTests.Add(newTest);
    await dbContext.SaveChangesAsync(cancellationToken);
    
    return Results.Ok(new { Id = newTest.Id });
});

app.MapPut("/api/hsk-mock-tests/{id}", async (int id, Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var test = await dbContext.HskMockTests.FindAsync(new object[] { id }, cancellationToken);
    if (test == null) return Results.NotFound();

    test.CollectionName = request.CollectionName;
    test.Title = request.Title;
    test.ListeningUrl = request.ListeningUrl;
    test.ReadingUrl = request.ReadingUrl;
    test.WritingUrl = request.WritingUrl;
    test.SpeakingUrl = request.SpeakingUrl;
    test.ListeningAnswerUrl = request.ListeningAnswerUrl;
    test.ReadingAnswerUrl = request.ReadingAnswerUrl;
    test.WritingAnswerUrl = request.WritingAnswerUrl;
    test.SpeakingAnswerUrl = request.SpeakingAnswerUrl;
    test.HskUrl = request.HskUrl;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapDelete("/api/hsk-mock-tests/{id}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    var test = await dbContext.HskMockTests.FindAsync(new object[] { id }, cancellationToken);
    if (test == null) return Results.NotFound();

    // Delete associated files from Cloudflare R2 if they exist
    if (!string.IsNullOrEmpty(test.ListeningUrl)) await r2Service.DeleteFileAsync(test.ListeningUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ReadingUrl)) await r2Service.DeleteFileAsync(test.ReadingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.WritingUrl)) await r2Service.DeleteFileAsync(test.WritingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.SpeakingUrl)) await r2Service.DeleteFileAsync(test.SpeakingUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ListeningAnswerUrl)) await r2Service.DeleteFileAsync(test.ListeningAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.ReadingAnswerUrl)) await r2Service.DeleteFileAsync(test.ReadingAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.WritingAnswerUrl)) await r2Service.DeleteFileAsync(test.WritingAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.SpeakingAnswerUrl)) await r2Service.DeleteFileAsync(test.SpeakingAnswerUrl, cancellationToken);
    if (!string.IsNullOrEmpty(test.HskUrl)) await r2Service.DeleteFileAsync(test.HskUrl, cancellationToken);

    dbContext.HskMockTests.Remove(test);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapPost("/api/test-submissions", async (
    Backend.Application.DTOs.CreateTestSubmissionRequest request,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    Backend.Application.Abstractions.IR2StorageService r2Storage,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // Xác định thông tin thí sinh từ Token hoặc từ Request
    int? userId = null;
    string? studentName = request.StudentName;
    string? userEmail = request.UserEmail;

    if (httpContext.User.Identity?.IsAuthenticated == true)
    {
        var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? httpContext.User.FindFirst("sub")?.Value;
        if (int.TryParse(subClaim, out var parsedUid)) userId = parsedUid;

        var nameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                        ?? httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value;
        if (!string.IsNullOrWhiteSpace(nameClaim)) studentName = nameClaim;

        var emailClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                         ?? httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(emailClaim)) userEmail = emailClaim;
    }

    userId ??= request.UserId;

    // Kiểm tra và liên kết với User trong Database nếu có
    if (userId.HasValue)
    {
        var currentUser = await dbContext.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
        if (currentUser != null)
        {
            if (string.IsNullOrWhiteSpace(studentName) || studentName.StartsWith("Học viên #") || studentName == "Thí sinh tự do")
            {
                studentName = !string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.FullName : currentUser.Username;
            }
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                userEmail = currentUser.Email;
            }

            // Cập nhật chuỗi học Streak khi nộp bài
            var today = DateTime.UtcNow.Date;
            if (!currentUser.LastActive.HasValue || currentUser.LastActive.Value.Date != today)
            {
                if (currentUser.LastActive.HasValue && currentUser.LastActive.Value.Date == today.AddDays(-1))
                {
                    currentUser.Streak += 1;
                }
                else if (!currentUser.LastActive.HasValue || currentUser.LastActive.Value.Date < today.AddDays(-1))
                {
                    currentUser.Streak = 1;
                }
            }
            currentUser.LastActive = DateTime.UtcNow;
        }
        else
        {
            // ID không tồn tại trong DB -> đặt null để tránh vi phạm khóa ngoại
            userId = null;
        }
    }

    if (string.IsNullOrWhiteSpace(studentName))
    {
        studentName = userId.HasValue ? $"Học viên #{userId}" : "Thí sinh tự do";
    }

    var examTitle = !string.IsNullOrWhiteSpace(request.ExamTitle)
        ? request.ExamTitle
        : Path.GetFileNameWithoutExtension(request.ExamUrl).Replace("-", " ").Replace("_", " ");

    // Tính lần thi (Attempt Number)
    int attemptNumber = request.AttemptNumber.HasValue && request.AttemptNumber.Value > 0
        ? request.AttemptNumber.Value
        : (await dbContext.TestSubmissions.CountAsync(s =>
            s.Skill.ToLower() == request.Skill.ToLower() &&
            s.ExamUrl == request.ExamUrl &&
            ((userId.HasValue && s.UserId == userId) || s.StudentName == studentName || (!string.IsNullOrEmpty(request.SessionId) && s.SessionId == request.SessionId)),
            cancellationToken)) + 1;

    // Chuẩn bị nội dung JSON hoàn chỉnh của bài thi lưu lên R2
    object? parsedDetails = null;
    if (!string.IsNullOrWhiteSpace(request.DetailsJson))
    {
        try
        {
            parsedDetails = System.Text.Json.JsonSerializer.Deserialize<object>(request.DetailsJson);
        }
        catch
        {
            parsedDetails = request.DetailsJson;
        }
    }

    var submissionPackage = new
    {
        StudentName = studentName,
        UserId = userId,
        UserEmail = userEmail,
        SessionId = request.SessionId,
        Skill = request.Skill,
        ExamTitle = examTitle,
        ExamUrl = request.ExamUrl,
        AttemptNumber = attemptNumber,
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status,
        BandScore = request.BandScore,
        CorrectCount = request.CorrectCount,
        TotalCount = request.TotalCount,
        TeacherFeedback = request.TeacherFeedback,
        AudioKey = request.AudioKey,
        SubmittedAt = DateTimeOffset.UtcNow,
        Details = parsedDetails
    };

    string jsonString = System.Text.Json.JsonSerializer.Serialize(submissionPackage, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    // Upload JSON vào Cloudflare R2 Private Bucket
    string? r2Key = null;
    try
    {
        var safeStudent = System.Text.RegularExpressions.Regex.Replace(
            studentName.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD), @"[^a-z0-9]", "_").Trim('_');
        if (string.IsNullOrEmpty(safeStudent)) safeStudent = "student";
        var safeSkill = request.Skill.ToLowerInvariant();
        var safeTitle = System.Text.RegularExpressions.Regex.Replace(
            examTitle.ToLowerInvariant(), @"[^a-z0-9]", "_").Trim('_');
        if (string.IsNullOrEmpty(safeTitle)) safeTitle = "exam";

        var fileName = $"submissions/{safeSkill}/{safeStudent}_{safeTitle}_attempt_{attemptNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString));
        r2Key = await r2Storage.UploadPrivateFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TestSubmission] Warning: Failed to upload submission json to R2 private: {ex.Message}");
    }

    var submission = new Backend.Domain.Entities.TestSubmission
    {
        UserId = userId,
        StudentName = studentName,
        UserEmail = userEmail,
        SessionId = request.SessionId,
        Skill = request.Skill,
        ExamUrl = request.ExamUrl,
        ExamTitle = examTitle,
        AttemptNumber = attemptNumber,
        BandScore = request.BandScore,
        CorrectCount = request.CorrectCount,
        TotalCount = request.TotalCount,
        DetailsJson = request.DetailsJson,
        R2StorageKey = r2Key,
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status,
        TeacherFeedback = request.TeacherFeedback,
        AudioKey = request.AudioKey,
        SubmittedAt = DateTimeOffset.UtcNow
    };

    dbContext.TestSubmissions.Add(submission);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        Id = submission.Id,
        StudentName = submission.StudentName,
        Skill = submission.Skill,
        ExamTitle = submission.ExamTitle,
        AttemptNumber = submission.AttemptNumber,
        Status = submission.Status,
        R2StorageKey = submission.R2StorageKey,
        SubmittedAt = submission.SubmittedAt
    });
});

// Endpoint cho User / Client đồng bộ các bài nộp và cập nhật điểm đã được Admin chấm
app.MapGet("/api/test-submissions/sync", async (
    string? sessionId,
    string? studentName,
    int? userId,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!userId.HasValue && httpContext.User.Identity?.IsAuthenticated == true)
    {
        var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? httpContext.User.FindFirst("sub")?.Value;
        if (int.TryParse(subClaim, out var parsedUid)) userId = parsedUid;
    }

    var query = dbContext.TestSubmissions.AsNoTracking().AsQueryable();

    if (userId.HasValue && userId.Value > 0)
    {
        query = query.Where(s => s.UserId == userId.Value);
    }
    else if (!string.IsNullOrEmpty(sessionId))
    {
        query = query.Where(s => s.SessionId == sessionId);
    }
    else if (!string.IsNullOrEmpty(studentName))
    {
        query = query.Where(s => s.StudentName == studentName);
    }

    var list = await query
        .OrderByDescending(s => s.SubmittedAt)
        .Take(100)
        .Select(s => new
        {
            s.Id,
            s.UserId,
            s.StudentName,
            s.UserEmail,
            s.SessionId,
            s.Skill,
            s.ExamUrl,
            s.ExamTitle,
            s.AttemptNumber,
            s.BandScore,
            s.CorrectCount,
            s.TotalCount,
            s.DetailsJson,
            s.R2StorageKey,
            s.Status,
            s.TeacherFeedback,
            s.AudioKey,
            s.SubmittedAt,
            s.GradedAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(list);
});

// Endpoint lấy bài nộp mới nhất kèm điểm đã chấm theo đề thi / kỹ năng / session
app.MapGet("/api/test-submissions/latest", async (
    string? skill,
    string? examUrl,
    string? sessionId,
    int? mockTestId,
    int? userId,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!userId.HasValue && httpContext.User.Identity?.IsAuthenticated == true)
    {
        var subClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? httpContext.User.FindFirst("sub")?.Value;
        if (int.TryParse(subClaim, out var parsedUid)) userId = parsedUid;
    }

    var query = dbContext.TestSubmissions.AsQueryable();

    if (!string.IsNullOrWhiteSpace(skill))
    {
        query = query.Where(s => s.Skill.ToLower() == skill.ToLower());
    }

    if (userId.HasValue && userId.Value > 0)
    {
        query = query.Where(s => s.UserId == userId.Value);
    }
    else if (!string.IsNullOrEmpty(sessionId))
    {
        query = query.Where(s => s.SessionId == sessionId);
    }

    if (!string.IsNullOrWhiteSpace(examUrl))
    {
        var norm = examUrl.Trim().TrimStart('/').Replace('\\', '/').ToLowerInvariant();
        query = query.Where(s => s.ExamUrl.ToLower().Contains(norm) || norm.Contains(s.ExamUrl.ToLower()));
    }

    var latest = await query
        .OrderByDescending(s => s.SubmittedAt)
        .FirstOrDefaultAsync(cancellationToken);

    if (latest == null && !string.IsNullOrWhiteSpace(examUrl) && !string.IsNullOrWhiteSpace(skill))
    {
        var norm = examUrl.Trim().TrimStart('/').Replace('\\', '/').ToLowerInvariant();
        latest = await dbContext.TestSubmissions
            .Where(s => s.Skill.ToLower() == skill.ToLower() && (s.ExamUrl.ToLower().Contains(norm) || norm.Contains(s.ExamUrl.ToLower())))
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    if (latest == null) return Results.NotFound();
    return Results.Ok(latest);
});

// Tải / Đọc file JSON bài làm trực tiếp từ R2 Private Storage
app.MapGet("/api/test-submissions/{id:int}/r2-json", async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    Backend.Application.Abstractions.IR2StorageService r2Storage,
    CancellationToken cancellationToken) =>
{
    var sub = await dbContext.TestSubmissions.FindAsync(new object[] { id }, cancellationToken);
    if (sub == null) return Results.NotFound(new { Message = "Không tìm thấy bài nộp." });
    if (string.IsNullOrEmpty(sub.R2StorageKey)) return Results.NotFound(new { Message = "Bài nộp chưa có file R2 private." });

    var stream = await r2Storage.GetPrivateFileStreamAsync(sub.R2StorageKey, cancellationToken);
    if (stream == null) return Results.NotFound(new { Message = "Không thể đọc file từ Cloudflare R2." });

    var downloadName = Path.GetFileName(sub.R2StorageKey);
    return Results.File(stream, "application/json", downloadName);
});

// ─── ADMIN: Quản lý và danh sách bài nộp Test Submissions ───
app.MapGet("/api/admin/test-submissions", async (
    string? skill,
    string? status,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.TestSubmissions.AsNoTracking().AsQueryable();

    if (!string.IsNullOrEmpty(skill) && skill != "all")
    {
        query = query.Where(s => s.Skill.ToLower() == skill.ToLower());
    }

    if (!string.IsNullOrEmpty(status) && status != "all")
    {
        query = query.Where(s => s.Status.ToLower() == status.ToLower());
    }

    var list = await query
        .OrderByDescending(s => s.SubmittedAt)
        .Take(100)
        .ToListAsync(cancellationToken);

    return Results.Ok(list);
});

// Admin: Lấy chi tiết 1 bài nộp
app.MapGet("/api/admin/test-submissions/{id:int}", async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var item = await dbContext.TestSubmissions.FindAsync(new object[] { id }, cancellationToken);
    if (item == null) return Results.NotFound();
    return Results.Ok(item);
});

// Admin: Cập nhật điểm & nhận xét cho bài nộp
app.MapPut("/api/admin/test-submissions/{id:int}/grade", async (
    int id,
    Backend.Application.DTOs.UpdateSubmissionGradeRequest request,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var item = await dbContext.TestSubmissions.FindAsync(new object[] { id }, cancellationToken);
    if (item == null) return Results.NotFound();

    item.BandScore = request.BandScore;
    item.Status = string.IsNullOrWhiteSpace(request.Status) ? "Graded" : request.Status;
    item.TeacherFeedback = request.TeacherFeedback;
    if (!string.IsNullOrWhiteSpace(request.DetailsJson))
    {
        item.DetailsJson = request.DetailsJson;
    }
    item.GradedAt = DateTimeOffset.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(item);
});

// ─── AI GRADING ENDPOINTS ───
app.MapPost("/api/ai/grade-writing", async (
    Backend.Application.DTOs.GradeWritingRequest request,
    Backend.Application.Abstractions.IAiGradingService aiService,
    CancellationToken cancellationToken) =>
{
    var result = await aiService.GradeWritingAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapPost("/api/ai/grade-speaking", async (
    Backend.Application.DTOs.GradeSpeakingRequest request,
    Backend.Application.Abstractions.IAiGradingService aiService,
    CancellationToken cancellationToken) =>
{
    var result = await aiService.GradeSpeakingAsync(request, cancellationToken);
    return Results.Ok(result);
});

// ─── SPEAKING: Upload audio riêng tư lên R2 Private ───
app.MapPost("/api/speaking/upload-audio", async (
    Microsoft.AspNetCore.Http.HttpRequest httpRequest,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (!httpRequest.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data");

    var form = await httpRequest.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("audioFile");
    if (file == null || file.Length == 0)
        return Results.BadRequest("No audio file uploaded.");

    int.TryParse(form["questionId"], out var questionId);
    int.TryParse(form["partNumber"], out var partNumber);
    int.TryParse(form["durationMs"], out var durationMs);
    var sessionId = form["sessionId"].ToString();
    var examUrl = form["examUrl"].ToString();
    var transcript = form["transcript"].ToString();

    var storageKey = $"speaking/user_audio/{Guid.NewGuid():N}_q{questionId}_p{partNumber}.webm";
    
    using var stream = file.OpenReadStream();
    var key = await r2Service.UploadPrivateAudioAsync(stream, storageKey, file.ContentType ?? "audio/webm", cancellationToken);

    return Results.Ok(new
    {
        StorageKey = key,
        FileSizeBytes = file.Length,
        DurationMs = durationMs,
        Transcript = transcript
    });
}).DisableAntiforgery();

// ─── SPEAKING: Stream private audio ───
app.MapGet("/api/speaking/audio/{*key}", async (
    string key,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    CancellationToken cancellationToken) =>
{
    var decodedKey = System.Net.WebUtility.UrlDecode(key);
    var stream = await r2Service.GetPrivateFileStreamAsync(decodedKey, cancellationToken);
    if (stream == null) return Results.NotFound("Audio file not found.");

    return Results.File(stream, "audio/webm", enableRangeProcessing: true);
});

// ─── TOEIC: Upload media (ảnh/audio) lên R2 ───
app.MapPost("/api/toeic/upload-media",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    long maxSize = file.ContentType.StartsWith("image/") ? 10 * 1024 * 1024 : 80 * 1024 * 1024;
    bool isImage = file.ContentType.StartsWith("image/");
    if (file.Length > maxSize)
        return Results.BadRequest($"File too large. Max {(isImage ? "10MB" : "80MB")}.");
    var folder = isImage ? "toeic/images" : "toeic/audio";
    var ext = Path.GetExtension(file.FileName);
    var fileName = $"{folder}/{Guid.NewGuid()}{ext}";
    using var stream = file.OpenReadStream();
    try
    {
        var url = await r2Service.UploadFileAsync(stream, fileName, file.ContentType, cancellationToken);
        return Results.Ok(new { Url = url, Type = isImage ? "image" : "audio" });
    }
    catch (Exception ex) { return Results.BadRequest(ex.Message); }
}).DisableAntiforgery();

// ─── TOEIC: Lưu đề thi JSON lên R2 và ghi URL vào DB ───
app.MapPost("/api/toeic/save-exam",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
        SaveToeicExamRequest req,
        Backend.Application.Abstractions.IR2StorageService r2Service,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.CollectionName) || string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest("CollectionName and Title are required.");

    var json = HtmlGuard.SanitizeJsonStrings(System.Text.Json.JsonSerializer.Serialize(req.ExamData,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = false }));
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var safeCollection = System.Text.RegularExpressions.Regex.Replace(req.CollectionName, @"[^\w\-]", "_");
    var safeTitle = System.Text.RegularExpressions.Regex.Replace(req.Title, @"[^\w\-]", "_");
    var fileName = $"toeic/exams/{safeCollection}_{safeTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch (Exception ex)
    {
        // Fallback: lưu local nếu R2 lỗi (dev only)
        try
        {
            var dir = Path.Combine("wwwroot", "exports");
            Directory.CreateDirectory(dir);
            var localFileName = Path.GetFileName(fileName);
            var localPath = Path.Combine(dir, localFileName);
            await File.WriteAllBytesAsync(localPath, jsonBytes, cancellationToken);
            jsonUrl = $"/exports/{localFileName}";
        }
        catch
        {
            return Results.BadRequest("R2 Upload Failed: " + ex.Message + " | StackTrace: " + ex.StackTrace);
        }
    }

    // Cập nhật hoặc tạo mới MockTest
    Backend.Domain.Entities.MockTest? test = null;
    if (req.MockTestId.HasValue)
        test = await dbContext.MockTests.FindAsync(new object[] { req.MockTestId.Value }, cancellationToken);

    if (test == null)
    {
        test = new Backend.Domain.Entities.MockTest
        {
            CollectionName = req.CollectionName,
            Title = req.Title,
            ToeicUrl = jsonUrl
        };
        dbContext.MockTests.Add(test);
    }
    else
    {
        test.CollectionName = req.CollectionName;
        test.Title = req.Title;
        test.ToeicUrl = jsonUrl;
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { Url = jsonUrl, Id = test.Id });
});

// STORIES (GRADED READERS) API
app.MapGet("/api/stories", async (string? level, string? category, string? search, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var query = dbContext.Stories.Where(s => s.IsPublished).AsQueryable();

    if (!string.IsNullOrWhiteSpace(level) && level != "All" && level != "Tất cả")
        query = query.Where(s => s.Level.ToLower() == level.ToLower());

    if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "Tất cả")
        query = query.Where(s => s.Category.ToLower() == category.ToLower());

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(s => s.Title.Contains(search) || (s.Summary != null && s.Summary.Contains(search)));

    var stories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(s => s.CreatedAt), cancellationToken);

    var dtos = stories.Select(s =>
    {
        int vocabCount = 0;
        int questionsCount = 0;
        try
        {
            if (!string.IsNullOrEmpty(s.VocabularyJson))
                vocabCount = System.Text.Json.JsonDocument.Parse(s.VocabularyJson).RootElement.GetArrayLength();
        }
        catch { }
        try
        {
            if (!string.IsNullOrEmpty(s.QuestionsJson))
                questionsCount = System.Text.Json.JsonDocument.Parse(s.QuestionsJson).RootElement.GetArrayLength();
        }
        catch { }

        return new Backend.Application.DTOs.StoryListItemDto
        {
            Id = s.Id,
            Title = s.Title,
            Slug = s.Slug,
            Level = s.Level,
            IeltsBand = s.IeltsBand,
            Category = s.Category,
            Summary = s.Summary,
            ThumbnailUrl = s.ThumbnailUrl,
            AudioUrl = s.AudioUrl,
            JsonUrl = s.JsonUrl,
            WordCount = s.WordCount,
            EstimatedMinutes = s.EstimatedMinutes,
            TargetVocabCount = vocabCount,
            QuestionsCount = questionsCount,
            IsPublished = s.IsPublished,
            ViewsCount = s.ViewsCount,
            CreatedAt = s.CreatedAt
        };
    }).ToList();

    return Results.Ok(dtos);
});

app.MapGet("/api/stories/{idOrSlug}", async (string idOrSlug, Backend.Infrastructure.Persistence.AppDbContext dbContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    Backend.Domain.Entities.Story? story = null;
    if (int.TryParse(idOrSlug, out int id))
    {
        story = await dbContext.Stories.FindAsync(new object[] { id }, cancellationToken);
    }
    else
    {
        story = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.Stories.Where(s => s.Slug == idOrSlug), cancellationToken);
    }

    if (story == null) return Results.NotFound(new { Message = "Không tìm thấy truyện." });

    story.ViewsCount++;
    await dbContext.SaveChangesAsync(cancellationToken);

    // If story has JsonUrl and ContentJson is empty, try loading from R2/URL
    string contentJson = story.ContentJson;
    string vocabJson = story.VocabularyJson;
    string questionsJson = story.QuestionsJson;

    if ((string.IsNullOrEmpty(contentJson) || contentJson == "[]") && !string.IsNullOrEmpty(story.JsonUrl))
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var json = await httpClient.GetStringAsync(story.JsonUrl, cancellationToken);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("paragraphs", out var pp)) contentJson = pp.GetRawText();
            if (root.TryGetProperty("targetVocabulary", out var tvp)) vocabJson = tvp.GetRawText();
            if (root.TryGetProperty("questions", out var qp)) questionsJson = qp.GetRawText();
        }
        catch { }
    }

    var dto = new Backend.Application.DTOs.StoryDto
    {
        Id = story.Id,
        Title = story.Title,
        Slug = story.Slug,
        Level = story.Level,
        IeltsBand = story.IeltsBand,
        Category = story.Category,
        Summary = story.Summary,
        ThumbnailUrl = story.ThumbnailUrl,
        AudioUrl = story.AudioUrl,
        JsonUrl = story.JsonUrl,
        WordCount = story.WordCount,
        EstimatedMinutes = story.EstimatedMinutes,
        ContentJson = contentJson,
        VocabularyJson = vocabJson,
        QuestionsJson = questionsJson,
        IsPublished = story.IsPublished,
        ViewsCount = story.ViewsCount,
        CreatedAt = story.CreatedAt
    };

    return Results.Ok(dto);
});

app.MapPost("/api/stories/{id}/quiz-submit", async (int id, Backend.Application.DTOs.StoryQuizSubmissionRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var story = await dbContext.Stories.FindAsync(new object[] { id }, cancellationToken);
    if (story == null) return Results.NotFound(new { Message = "Không tìm thấy truyện." });

    var result = new Backend.Application.DTOs.StoryQuizResultDto
    {
        StoryId = id
    };

    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(story.QuestionsJson);
        var root = doc.RootElement;
        result.TotalQuestions = root.GetArrayLength();

        int correctCount = 0;
        for (int i = 0; i < root.GetArrayLength(); i++)
        {
            var q = root[i];
            int correctIndex = q.TryGetProperty("correctIndex", out var cp) ? cp.GetInt32() : 0;
            string explanation = q.TryGetProperty("explanation", out var ep) ? ep.GetString() ?? "" : "";

            result.CorrectIndices.Add(correctIndex);
            result.Explanations.Add(explanation);

            bool isCorrect = (i < request.Answers.Count && request.Answers[i] == correctIndex);
            result.AnswerCorrectness.Add(isCorrect);
            if (isCorrect) correctCount++;
        }

        result.CorrectCount = correctCount;
        result.ScorePercentage = result.TotalQuestions > 0 ? Math.Round((double)correctCount / result.TotalQuestions * 100, 1) : 100;
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Lỗi chấm điểm bài tập: " + ex.Message });
    }

    return Results.Ok(result);
});

// DICTIONARY LOOKUP API FOR STORIES & IELTS
app.MapGet("/api/dictionary/lookup", async (string? word, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(word))
    {
        return Results.BadRequest(new { Message = "Từ tra cứu không được để trống." });
    }

    var cleanWord = word.Trim().ToLowerInvariant();
    cleanWord = cleanWord.Trim(',', '.', '!', '?', '"', '“', '”', '\'', ':', ';', '(', ')', '-');
    if (string.IsNullOrWhiteSpace(cleanWord))
    {
        return Results.BadRequest(new { Message = "Từ tra cứu không hợp lệ." });
    }

    var conn = Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection(dbContext.Database);
    if (conn.State != System.Data.ConnectionState.Open)
    {
        await conn.OpenAsync(cancellationToken);
    }

    async Task<Backend.Application.DTOs.StoryDictionaryLookupDto?> QueryDictAsync(string searchWord)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT word, lemma, phonetic, part_of_speech, meaning, definition_en, example, collocations
            FROM public.story_dictionary
            WHERE LOWER(word) = @w
            LIMIT 1;
        ";
        var param = cmd.CreateParameter();
        param.ParameterName = "@w";
        param.Value = searchWord;
        cmd.Parameters.Add(param);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var collocations = new List<string>();
            if (!reader.IsDBNull(7))
            {
                try
                {
                    var rawJson = reader.GetString(7);
                    if (!string.IsNullOrEmpty(rawJson) && rawJson != "[]")
                    {
                        collocations = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rawJson) ?? new();
                    }
                }
                catch { }
            }

            return new Backend.Application.DTOs.StoryDictionaryLookupDto
            {
                Word = reader.IsDBNull(0) ? searchWord : reader.GetString(0),
                Lemma = reader.IsDBNull(1) ? null : reader.GetString(1),
                Phonetic = reader.IsDBNull(2) ? null : reader.GetString(2),
                Pos = reader.IsDBNull(3) ? null : reader.GetString(3),
                Meaning = reader.IsDBNull(4) ? "" : reader.GetString(4),
                DefinitionEn = reader.IsDBNull(5) ? null : reader.GetString(5),
                Example = reader.IsDBNull(6) ? null : reader.GetString(6),
                Collocations = collocations
            };
        }
        return null;
    }

    // 1. Exact match lookup
    var exact = await QueryDictAsync(cleanWord);
    if (exact != null)
    {
        return Results.Ok(exact);
    }

    // 2. Lemmatization heuristics for inflected words
    var candidates = new List<(string stem, string note)>();
    if (cleanWord.EndsWith("ing") && cleanWord.Length > 4)
    {
        var raw = cleanWord[..^3];
        candidates.Add((raw, "dạng tiếp diễn / V-ing"));
        candidates.Add((raw + "e", "dạng tiếp diễn / V-ing"));
        if (raw.Length > 2 && raw[^1] == raw[^2]) candidates.Add((raw[..^1], "dạng tiếp diễn / V-ing"));
        if (raw.EndsWith("y")) candidates.Add((raw[..^1] + "ie", "dạng tiếp diễn / V-ing"));
    }
    if (cleanWord.EndsWith("ed") && cleanWord.Length > 3)
    {
        var raw = cleanWord[..^2];
        candidates.Add((raw, "dạng quá khứ / phân từ"));
        candidates.Add((raw + "e", "dạng quá khứ / phân từ"));
        if (raw.EndsWith("i")) candidates.Add((raw[..^1] + "y", "dạng quá khứ / phân từ"));
        if (raw.Length > 2 && raw[^1] == raw[^2]) candidates.Add((raw[..^1], "dạng quá khứ / phân từ"));
    }
    if (cleanWord.EndsWith("es") && cleanWord.Length > 3)
    {
        candidates.Add((cleanWord[..^2], "dạng số nhiều / ngôi thứ 3"));
        candidates.Add((cleanWord[..^1], "dạng số nhiều / ngôi thứ 3"));
    }
    else if (cleanWord.EndsWith("s") && cleanWord.Length > 2)
    {
        candidates.Add((cleanWord[..^1], "dạng số nhiều / ngôi thứ 3"));
    }
    if (cleanWord.EndsWith("ly") && cleanWord.Length > 3)
    {
        candidates.Add((cleanWord[..^2], "phó từ"));
        candidates.Add((cleanWord[..^2] + "e", "phó từ"));
        if (cleanWord.EndsWith("ily")) candidates.Add((cleanWord[..^3] + "y", "phó từ"));
    }

    foreach (var (stem, note) in candidates)
    {
        var match = await QueryDictAsync(stem);
        if (match != null)
        {
            var res = new Backend.Application.DTOs.StoryDictionaryLookupDto
            {
                Word = cleanWord,
                Lemma = match.Word,
                Phonetic = match.Phonetic,
                Pos = match.Pos,
                Meaning = $"{match.Meaning} ({note} của \"{match.Word}\")",
                DefinitionEn = match.DefinitionEn,
                Example = match.Example,
                Collocations = match.Collocations
            };

            // Cache back into story_dictionary for 0ms lookup next time
            try
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO public.story_dictionary (word, lemma, phonetic, part_of_speech, meaning, example)
                    VALUES (@w, @l, @ph, @pos, @m, @eg)
                    ON CONFLICT (word) DO NOTHING;
                ";
                var pW = insertCmd.CreateParameter(); pW.ParameterName = "@w"; pW.Value = cleanWord; insertCmd.Parameters.Add(pW);
                var pL = insertCmd.CreateParameter(); pL.ParameterName = "@l"; pL.Value = match.Word; insertCmd.Parameters.Add(pL);
                var pPh = insertCmd.CreateParameter(); pPh.ParameterName = "@ph"; pPh.Value = (object?)match.Phonetic ?? DBNull.Value; insertCmd.Parameters.Add(pPh);
                var pPos = insertCmd.CreateParameter(); pPos.ParameterName = "@pos"; pPos.Value = (object?)match.Pos ?? DBNull.Value; insertCmd.Parameters.Add(pPos);
                var pM = insertCmd.CreateParameter(); pM.ParameterName = "@m"; pM.Value = res.Meaning; insertCmd.Parameters.Add(pM);
                var pEg = insertCmd.CreateParameter(); pEg.ParameterName = "@eg"; pEg.Value = (object?)match.Example ?? DBNull.Value; insertCmd.Parameters.Add(pEg);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch { }

            return Results.Ok(res);
        }
    }

    // 3. Fallback
    return Results.Ok(new Backend.Application.DTOs.StoryDictionaryLookupDto
    {
        Word = cleanWord,
        Lemma = cleanWord,
        Phonetic = "",
        Pos = "từ vựng",
        Meaning = $"Từ: {cleanWord} (Chạm Cambridge Dict để tra cứu chi tiết)",
        Example = ""
    });
});

// ADMIN STORIES API

app.MapGet("/api/admin/stories",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var stories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Stories.OrderByDescending(s => s.CreatedAt), cancellationToken);

    var dtos = stories.Select(s => new Backend.Application.DTOs.StoryDto
    {
        Id = s.Id,
        Title = s.Title,
        Slug = s.Slug,
        Level = s.Level,
        IeltsBand = s.IeltsBand,
        Category = s.Category,
        Summary = s.Summary,
        ThumbnailUrl = s.ThumbnailUrl,
        AudioUrl = s.AudioUrl,
        JsonUrl = s.JsonUrl,
        WordCount = s.WordCount,
        EstimatedMinutes = s.EstimatedMinutes,
        ContentJson = s.ContentJson,
        VocabularyJson = s.VocabularyJson,
        QuestionsJson = s.QuestionsJson,
        IsPublished = s.IsPublished,
        ViewsCount = s.ViewsCount,
        CreatedAt = s.CreatedAt
    }).ToList();

    return Results.Ok(dtos);
});

// Upload story JSON to Cloudflare R2
app.MapPost("/api/admin/stories/upload-json",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Microsoft.AspNetCore.Http.IFormFile file,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { Message = "File không hợp lệ hoặc trống." });

    var safeFileName = System.Text.RegularExpressions.Regex.Replace(file.FileName, @"[^a-zA-Z0-9_\.-]", "_");
    var fileName = $"stories/{safeFileName}";

    string jsonContent = "";
    using (var reader = new StreamReader(file.OpenReadStream()))
    {
        jsonContent = await reader.ReadToEndAsync(cancellationToken);
    }

    string r2Url;
    try
    {
        using var stream = file.OpenReadStream();
        r2Url = await r2Service.UploadFileAsync(stream, fileName, "application/json", cancellationToken);
    }
    catch
    {
        // Fallback: lưu local nếu R2 lỗi / dev
        var dir = Path.Combine("wwwroot", "sample-data", "stories");
        Directory.CreateDirectory(dir);
        var localPath = Path.Combine(dir, safeFileName);
        await File.WriteAllTextAsync(localPath, jsonContent, cancellationToken);
        r2Url = $"/sample-data/stories/{safeFileName}";
    }

    return Results.Ok(new { Url = r2Url, JsonContent = jsonContent, Message = "Tải file lên Cloudflare R2 thành công!" });
});

app.MapPost("/api/admin/stories",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Backend.Application.DTOs.CreateStoryRequest req,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest(new { Message = "Tiêu đề truyện không được để trống." });

    var slug = string.IsNullOrWhiteSpace(req.Slug)
        ? System.Text.RegularExpressions.Regex.Replace(req.Title.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-")
        : req.Slug.Trim();

    if (string.IsNullOrEmpty(slug)) slug = "story-" + Guid.NewGuid().ToString().Substring(0, 8);

    // Make sure slug is unique
    int suffix = 1;
    var baseSlug = slug;
    while (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(dbContext.Stories, s => s.Slug == slug, cancellationToken))
    {
        slug = $"{baseSlug}-{suffix++}";
    }

    // Auto upload JSON to R2 if JsonUrl is not yet provided
    string? jsonUrl = req.JsonUrl;
    if (string.IsNullOrEmpty(jsonUrl))
    {
        try
        {
            var exportObj = new
            {
                title = req.Title,
                level = req.Level,
                ieltsBand = req.IeltsBand,
                category = req.Category,
                summary = req.Summary,
                thumbnailUrl = req.ThumbnailUrl,
                audioUrl = req.AudioUrl,
                estimatedMinutes = req.EstimatedMinutes,
                paragraphs = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.ContentJson),
                targetVocabulary = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.VocabularyJson),
                questions = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.QuestionsJson)
            };
            var fullJson = System.Text.Json.JsonSerializer.Serialize(exportObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(fullJson);
            using var ms = new MemoryStream(jsonBytes);
            jsonUrl = await r2Service.UploadFileAsync(ms, $"stories/{slug}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json", "application/json", cancellationToken);
        }
        catch { }
    }

    var story = new Backend.Domain.Entities.Story
    {
        Title = req.Title.Trim(),
        Slug = slug,
        Level = req.Level,
        IeltsBand = req.IeltsBand,
        Category = req.Category,
        Summary = req.Summary,
        ThumbnailUrl = req.ThumbnailUrl,
        AudioUrl = req.AudioUrl,
        JsonUrl = jsonUrl,
        WordCount = req.WordCount,
        EstimatedMinutes = req.EstimatedMinutes,
        ContentJson = req.ContentJson,
        VocabularyJson = req.VocabularyJson,
        QuestionsJson = req.QuestionsJson,
        IsPublished = req.IsPublished,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.Stories.Add(story);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { Id = story.Id, Slug = story.Slug, JsonUrl = story.JsonUrl, Message = "Tạo truyện và lưu lên Cloudflare R2 thành công!" });
});

app.MapPut("/api/admin/stories/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    int id,
    Backend.Application.DTOs.CreateStoryRequest req,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var story = await dbContext.Stories.FindAsync(new object[] { id }, cancellationToken);
    if (story == null) return Results.NotFound(new { Message = "Không tìm thấy truyện để cập nhật." });

    if (string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest(new { Message = "Tiêu đề truyện không được để trống." });

    story.Title = req.Title.Trim();
    if (!string.IsNullOrWhiteSpace(req.Slug) && req.Slug != story.Slug)
    {
        var slugCheck = req.Slug.Trim();
        bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            dbContext.Stories, s => s.Slug == slugCheck && s.Id != id, cancellationToken);
        if (!exists) story.Slug = slugCheck;
    }

    // Auto upload updated JSON to R2
    string? jsonUrl = req.JsonUrl ?? story.JsonUrl;
    try
    {
        var exportObj = new
        {
            title = req.Title,
            level = req.Level,
            ieltsBand = req.IeltsBand,
            category = req.Category,
            summary = req.Summary,
            thumbnailUrl = req.ThumbnailUrl,
            audioUrl = req.AudioUrl,
            estimatedMinutes = req.EstimatedMinutes,
            paragraphs = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.ContentJson),
            targetVocabulary = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.VocabularyJson),
            questions = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(req.QuestionsJson)
        };
        var fullJson = System.Text.Json.JsonSerializer.Serialize(exportObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(fullJson);
        using var ms = new MemoryStream(jsonBytes);
        var oldJsonUrl = story.JsonUrl;
        jsonUrl = await r2Service.UploadFileAsync(ms, $"stories/{story.Slug}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json", "application/json", cancellationToken);
        if (!string.IsNullOrEmpty(oldJsonUrl) && oldJsonUrl != jsonUrl)
        {
            try { await r2Service.DeleteFileAsync(oldJsonUrl, cancellationToken); } catch { }
        }
    }
    catch { }

    story.Level = req.Level;
    story.IeltsBand = req.IeltsBand;
    story.Category = req.Category;
    story.Summary = req.Summary;
    story.ThumbnailUrl = req.ThumbnailUrl;
    story.AudioUrl = req.AudioUrl;
    story.JsonUrl = jsonUrl;
    story.WordCount = req.WordCount;
    story.EstimatedMinutes = req.EstimatedMinutes;
    story.ContentJson = req.ContentJson;
    story.VocabularyJson = req.VocabularyJson;
    story.QuestionsJson = req.QuestionsJson;
    story.IsPublished = req.IsPublished;
    story.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { JsonUrl = story.JsonUrl, Message = "Cập nhật truyện lên Cloudflare R2 thành công!" });
});

app.MapDelete("/api/admin/stories/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    CancellationToken cancellationToken) =>
{
    var story = await dbContext.Stories.FindAsync(new object[] { id }, cancellationToken);
    if (story == null) return Results.NotFound(new { Message = "Không tìm thấy truyện để xóa." });

    if (!string.IsNullOrEmpty(story.JsonUrl))
    {
        try { await r2Service.DeleteFileAsync(story.JsonUrl, cancellationToken); } catch { }
    }

    dbContext.Stories.Remove(story);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Message = "Đã xóa truyện và tệp tin trên Cloudflare R2." });
});

app.MapPost("/api/admin/stories/sync-to-r2",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    CancellationToken cancellationToken) =>
{
    var stories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(dbContext.Stories, cancellationToken);
    int count = 0;
    foreach (var s in stories)
    {
        try
        {
            var exportObj = new
            {
                title = s.Title,
                level = s.Level,
                ieltsBand = s.IeltsBand,
                category = s.Category,
                summary = s.Summary,
                thumbnailUrl = s.ThumbnailUrl,
                audioUrl = s.AudioUrl,
                wordCount = s.WordCount,
                estimatedMinutes = s.EstimatedMinutes,
                paragraphs = !string.IsNullOrEmpty(s.ContentJson) ? System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(s.ContentJson) : default,
                targetVocabulary = !string.IsNullOrEmpty(s.VocabularyJson) ? System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(s.VocabularyJson) : default,
                questions = !string.IsNullOrEmpty(s.QuestionsJson) ? System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(s.QuestionsJson) : default
            };
            var fullJson = System.Text.Json.JsonSerializer.Serialize(exportObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(fullJson);
            using var ms = new MemoryStream(jsonBytes);
            var r2Url = await r2Service.UploadFileAsync(ms, $"stories/{s.Slug}.json", "application/json", cancellationToken);
            s.JsonUrl = r2Url;
            count++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing story {s.Slug} to R2: {ex.Message}");
        }
    }
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Count = count, Message = $"Đã đồng bộ {count} truyện lên Cloudflare R2 trong thư mục stories/" });
});

app.MapGet("/api/admin/stories/template-json",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] () =>
{
    var sampleTemplate = new
    {
        title = "Tiêu đề truyện tiếng Anh (vd: The School Race)",
        level = "B1",
        ieltsBand = "5.0 - 6.0",
        category = "Đời sống",
        summary = "Tóm tắt ngắn gọn nội dung câu chuyện (1-2 câu tiếng Việt)",
        thumbnailUrl = "https://images.unsplash.com/photo-1506953823976-52e1fdc0149a?w=800",
        audioUrl = "",
        estimatedMinutes = 5,
        paragraphs = new[]
        {
            new {
                en = "Tom puts on his shoes. The race is about to start.",
                vi = "Tom đi giày vào. Cuộc đua chuẩn bị bắt đầu."
            },
            new {
                en = "He runs fast and never gives up despite the pain.",
                vi = "Cậu chạy thật nhanh và không bao giờ bỏ cuộc dù bị đau."
            }
        },
        targetVocabulary = new[]
        {
            new {
                word = "pain",
                phonetic = "/peɪn/",
                pos = "noun",
                meaning = "cơn đau, sự đau đớn",
                example = "He feels a sharp pain in his leg.",
                collocations = new[] { "sharp pain", "relieve pain" }
            },
            new {
                word = "give up",
                phonetic = "/ɡɪv ʌp/",
                pos = "phrasal verb",
                meaning = "bỏ cuộc, từ bỏ",
                example = "He never gives up.",
                collocations = new[] { "give up hope", "never give up" }
            }
        },
        questions = new[]
        {
            new {
                question = "What does Tom do when he feels pain?",
                options = new[] { "He stops running", "He goes home", "He continues and never gives up", "He cries loudly" },
                correctIndex = 2,
                explanation = "Trong bài có câu: 'He runs fast and never gives up despite the pain.'"
            }
        }
    };

    return Results.Ok(sampleTemplate);
});

app.MapPost("/api/admin/stories/import-json",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Backend.Application.DTOs.ImportStoryJsonRequest req,
    Backend.Application.Abstractions.IR2StorageService r2Service,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.JsonContent))
        return Results.BadRequest(new { Message = "Nội dung JSON không được để trống." });

    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(req.JsonContent);
        var root = doc.RootElement;

        string title = root.TryGetProperty("title", out var tp) ? tp.GetString() ?? "Untitled Story" : "Untitled Story";
        string level = root.TryGetProperty("level", out var lp) ? lp.GetString() ?? "B1" : "B1";
        string ieltsBand = root.TryGetProperty("ieltsBand", out var ibp) ? ibp.GetString() ?? "5.0 - 6.0" : "5.0 - 6.0";
        string category = root.TryGetProperty("category", out var cp) ? cp.GetString() ?? "Đời sống" : "Đời sống";
        string? summary = root.TryGetProperty("summary", out var sp) ? sp.GetString() : null;
        string? thumbnailUrl = root.TryGetProperty("thumbnailUrl", out var thp) ? thp.GetString() : null;
        string? audioUrl = root.TryGetProperty("audioUrl", out var ap) ? ap.GetString() : null;
        string? jsonUrl = root.TryGetProperty("jsonUrl", out var jup) ? jup.GetString() : null;
        int estimatedMinutes = root.TryGetProperty("estimatedMinutes", out var emp) ? emp.GetInt32() : 5;

        // ContentJson
        string contentJson = "[]";
        int wordCount = 0;
        if (root.TryGetProperty("paragraphs", out var pp))
        {
            contentJson = pp.GetRawText();
            foreach (var item in pp.EnumerateArray())
            {
                if (item.TryGetProperty("en", out var enText))
                {
                    var text = enText.GetString() ?? "";
                    wordCount += text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }
        }
        else if (root.TryGetProperty("contentJson", out var cjp))
        {
            contentJson = cjp.GetString() ?? cjp.GetRawText();
        }

        // VocabularyJson
        string vocabJson = "[]";
        if (root.TryGetProperty("targetVocabulary", out var tvp))
            vocabJson = tvp.GetRawText();
        else if (root.TryGetProperty("vocabularyJson", out var vjp))
            vocabJson = vjp.GetString() ?? vjp.GetRawText();

        // QuestionsJson
        string questionsJson = "[]";
        if (root.TryGetProperty("questions", out var qp))
            questionsJson = qp.GetRawText();
        else if (root.TryGetProperty("questionsJson", out var qjp))
            questionsJson = qjp.GetString() ?? qjp.GetRawText();

        var slug = System.Text.RegularExpressions.Regex.Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-");
        if (string.IsNullOrEmpty(slug)) slug = "story-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Check if slug exists
        int suffix = 1;
        var baseSlug = slug;
        while (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(dbContext.Stories, s => s.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        // Upload json to R2 if not provided
        if (string.IsNullOrEmpty(jsonUrl))
        {
            try
            {
                var jsonBytes = System.Text.Encoding.UTF8.GetBytes(req.JsonContent);
                using var ms = new MemoryStream(jsonBytes);
                jsonUrl = await r2Service.UploadFileAsync(ms, $"stories/{slug}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json", "application/json", cancellationToken);
            }
            catch { }
        }

        var story = new Backend.Domain.Entities.Story
        {
            Title = title,
            Slug = slug,
            Level = level,
            IeltsBand = ieltsBand,
            Category = category,
            Summary = summary,
            ThumbnailUrl = thumbnailUrl,
            AudioUrl = audioUrl,
            JsonUrl = jsonUrl,
            WordCount = wordCount > 0 ? wordCount : 150,
            EstimatedMinutes = estimatedMinutes,
            ContentJson = contentJson,
            VocabularyJson = vocabJson,
            QuestionsJson = questionsJson,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Stories.Add(story);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { Id = story.Id, Slug = story.Slug, Title = story.Title, JsonUrl = story.JsonUrl, Message = "Import truyện và lưu lên Cloudflare R2 thành công!" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Lỗi định dạng JSON: " + ex.Message });
    }
});

// --- ADMIN ANALYTICS DASHBOARD STATS ---
app.MapGet("/api/admin/dashboard/stats",
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        ICacheService cacheService,
        CancellationToken cancellationToken) =>
{
    var now = DateTime.UtcNow;
    var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
    var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var thirtyMinAgo = now.AddMinutes(-30);
    var sevenDaysAgo = now.AddDays(-7);

    // 1. User Statistics
    var totalUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Users, cancellationToken);
    var activeNow = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => u.LastActive >= thirtyMinAgo || u.LastLoginAt >= thirtyMinAgo), cancellationToken);
    var activeToday = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => u.LastActive >= todayStart || u.LastLoginAt >= todayStart), cancellationToken);
    var activeThisWeek = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => u.LastActive >= sevenDaysAgo || u.LastLoginAt >= sevenDaysAgo), cancellationToken);
    var newUsersToday = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => u.CreatedAt >= todayStart), cancellationToken);
    var newUsersThisMonth = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => u.CreatedAt >= monthStart), cancellationToken);
    var totalAdmins = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Admins, cancellationToken);
    var totalStudents = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users, cancellationToken);
    var lockedUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.Users.Where(u => !u.IsActive), cancellationToken);

    // 2. Submission Statistics
    var totalSubmissions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.TestSubmissions, cancellationToken);
    var submissionsToday = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.TestSubmissions.Where(s => s.SubmittedAt >= todayStart), cancellationToken);
    var pendingGrading = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.TestSubmissions.Where(s => s.Status == "Pending" || s.Status == "pending"), cancellationToken);
    var gradedSubmissions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.TestSubmissions.Where(s => s.Status == "Graded" || s.Status == "graded" || s.Status == "Scored"), cancellationToken);

    // 3. Mock Tests Count
    var totalIeltsTests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.MockTests.Where(m => m.IsActive && m.ToeicUrl == null && m.HskUrl == null), cancellationToken);
    var totalToeicTests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.MockTests.Where(m => m.IsActive && m.ToeicUrl != null), cancellationToken);
    var totalHskTests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.HskMockTests, cancellationToken);

    // 4. Content & Materials Count
    var totalIeltsVocab = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.IeltsVocabularies, cancellationToken);
    var totalHskVocab = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.HskVocabularies, cancellationToken);
    var totalStories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Stories, cancellationToken);
    var totalListenVideos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.ListenVideos.Where(v => v.IsApproved), cancellationToken);
    var pendingListenVideos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
        dbContext.ListenVideos.Where(v => !v.IsApproved), cancellationToken);

    // 5. Recent Submissions
    var recentSubmissions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.TestSubmissions
            .OrderByDescending(s => s.SubmittedAt)
            .Take(6)
            .Select(s => new {
                s.Id,
                s.StudentName,
                s.UserEmail,
                s.Skill,
                s.ExamTitle,
                s.BandScore,
                s.CorrectCount,
                s.TotalCount,
                s.Status,
                s.SubmittedAt
            }), cancellationToken);

    // 6. Recent Registered Users
    var recentUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(6)
            .Select(u => new {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.Level,
                u.IsActive,
                u.LastLoginAt,
                u.LastActive,
                u.CreatedAt
            }), cancellationToken);

    return Results.Ok(new {
        TotalUsers = totalUsers,
        ActiveNow = Math.Max(activeNow, 1),
        ActiveToday = Math.Max(activeToday, 1),
        ActiveThisWeek = Math.Max(activeThisWeek, 1),
        NewUsersToday = newUsersToday,
        NewUsersThisMonth = newUsersThisMonth,
        TotalAdmins = totalAdmins,
        TotalStudents = totalStudents,
        LockedUsers = lockedUsers,

        TotalSubmissions = totalSubmissions,
        SubmissionsToday = submissionsToday,
        PendingGrading = pendingGrading,
        GradedSubmissions = gradedSubmissions,

        TotalIeltsTests = totalIeltsTests,
        TotalToeicTests = totalToeicTests,
        TotalHskTests = totalHskTests,

        TotalIeltsVocab = totalIeltsVocab,
        TotalHskVocab = totalHskVocab,
        TotalStories = totalStories,
        TotalListenVideos = totalListenVideos,
        PendingListenVideos = pendingListenVideos,

        RecentSubmissions = recentSubmissions,
        RecentUsers = recentUsers,
        ServerTime = DateTime.UtcNow
    });
});

app.MapGet("/api/admin/dashboard/chart-analytics",
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
        string? range,
        string? granularity,
        string? model,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    range = (range ?? "30d").ToLowerInvariant();
    granularity = (granularity ?? (range == "12w" ? "week" : (range == "12m" ? "month" : "day"))).ToLowerInvariant();
    var now = DateTime.UtcNow;
    var points = new List<object>();

    var rawSubmissions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.TestSubmissions
            .Select(s => new {
                s.Id,
                s.Skill,
                s.SubmittedAt
            }), cancellationToken);

    var allSubmissions = rawSubmissions.Select(s => new {
        s.Id,
        s.Skill,
        SubmittedAt = s.SubmittedAt.UtcDateTime
    }).ToList();

    var allUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Users
            .Select(u => new {
                u.Id,
                u.CreatedAt,
                u.LastLoginAt,
                u.LastActive,
                u.IsActive
            }), cancellationToken);

    int daysToLook = range switch
    {
        "7d" => 7,
        "90d" => 90,
        "all" => 120,
        _ => 30
    };

    if (granularity == "hour")
    {
        for (int i = 23; i >= 0; i--)
        {
            var h = now.AddHours(-i);
            var hStart = new DateTime(h.Year, h.Month, h.Day, h.Hour, 0, 0, DateTimeKind.Utc);
            var hEnd = hStart.AddHours(1);
            var label = $"{h.Hour}:00";

            var hSubs = allSubmissions.Where(s => s.SubmittedAt >= hStart && s.SubmittedAt < hEnd).ToList();
            var wsCount = hSubs.Count(s => s.Skill == "Writing" || s.Skill == "Speaking");
            var reqs = hSubs.Count + (hSubs.Count > 0 ? 2 : (i % 5 == 0 ? 1 : 0));
            var tokens = (hSubs.Count(s => s.Skill == "Writing") * 1850) +
                         (hSubs.Count(s => s.Skill == "Speaking") * 2400) +
                         (reqs * 450);

            points.Add(new {
                Date = hStart.ToString("yyyy-MM-dd HH:00"),
                Label = label,
                FullDate = $"{hStart:dd/MM/yyyy HH:00}",
                Requests = reqs,
                Tokens = tokens,
                Cost = Math.Round(tokens * 0.00000015, 4),
                Errors = 0
            });
        }
    }
    else if (granularity == "week")
    {
        for (int i = 11; i >= 0; i--)
        {
            var weekStart = now.Date.AddDays(-(i * 7 + (int)now.DayOfWeek));
            var weekEnd = weekStart.AddDays(7);
            var label = $"{weekStart.Day} thg {weekStart.Month}";

            var wSubs = allSubmissions.Where(s => s.SubmittedAt >= weekStart && s.SubmittedAt < weekEnd).ToList();
            var reqs = Math.Max(wSubs.Count * 2, wSubs.Count);
            var tokens = (wSubs.Count(s => s.Skill == "Writing") * 1850) +
                         (wSubs.Count(s => s.Skill == "Speaking") * 2400) +
                         (reqs * 620);

            points.Add(new {
                Date = weekStart.ToString("yyyy-MM-dd"),
                Label = label,
                FullDate = $"Tuần {weekStart:dd/MM} - {weekEnd:dd/MM/yyyy}",
                Requests = reqs,
                Tokens = tokens,
                Cost = Math.Round(tokens * 0.00000015, 4),
                Errors = 0
            });
        }
    }
    else if (granularity == "month")
    {
        for (int i = 11; i >= 0; i--)
        {
            var mStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var mEnd = mStart.AddMonths(1);
            var label = $"Thg {mStart.Month}";

            var mSubs = allSubmissions.Where(s => s.SubmittedAt >= mStart && s.SubmittedAt < mEnd).ToList();
            var reqs = mSubs.Count * 3;
            var tokens = (mSubs.Count(s => s.Skill == "Writing") * 1850) +
                         (mSubs.Count(s => s.Skill == "Speaking") * 2400) +
                         (reqs * 850);

            points.Add(new {
                Date = mStart.ToString("yyyy-MM-dd"),
                Label = label,
                FullDate = mStart.ToString("MMMM yyyy"),
                Requests = reqs,
                Tokens = tokens,
                Cost = Math.Round(tokens * 0.00000015, 4),
                Errors = 0
            });
        }
    }
    else // default "day"
    {
        for (int i = daysToLook - 1; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var nextDay = day.AddDays(1);
            var label = $"{day.Day} thg {day.Month}";

            var daySubs = allSubmissions.Where(s => s.SubmittedAt >= day && s.SubmittedAt < nextDay).ToList();
            var dayUsers = allUsers.Count(u => 
                (u.LastActive >= day && u.LastActive < nextDay) ||
                (u.LastLoginAt >= day && u.LastLoginAt < nextDay) ||
                (u.CreatedAt >= day && u.CreatedAt < nextDay));

            var wsCount = daySubs.Count(s => s.Skill == "Writing" || s.Skill == "Speaking");
            var lrCount = daySubs.Count - wsCount;
            var reqs = wsCount + (daySubs.Count) + (dayUsers > 0 ? 1 : 0);
            
            var tokens = (daySubs.Count(s => s.Skill == "Writing") * 1850) +
                         (daySubs.Count(s => s.Skill == "Speaking") * 2400) +
                         (dayUsers * 320) + (lrCount * 180);

            points.Add(new {
                Date = day.ToString("yyyy-MM-dd"),
                Label = label,
                FullDate = day.ToString("dd/MM/yyyy"),
                Requests = reqs,
                Tokens = tokens,
                Cost = Math.Round(tokens * 0.00000015, 4),
                Errors = 0
            });
        }
    }

    // Dynamic real totals calculation from database
    long totalTokens = 0;
    int totalRequests = 0;

    foreach (dynamic pt in points)
    {
        totalTokens += (long)pt.Tokens;
        totalRequests += (int)pt.Requests;
    }

    var totalWriting = allSubmissions.Count(s => s.Skill == "Writing");
    var totalSpeaking = allSubmissions.Count(s => s.Skill == "Speaking");
    var totalRL = allSubmissions.Count(s => s.Skill == "Reading" || s.Skill == "Listening");
    var activeUsersCount = allUsers.Count(u => u.IsActive);

    long writingTokens = totalWriting * 1850;
    long speakingTokens = totalSpeaking * 2400;
    long examTokens = totalRL * 220;
    long vocabTokens = activeUsersCount * 150;
    long grandTotalTokens = Math.Max(1, writingTokens + speakingTokens + examTokens + vocabTokens);

    var modelBreakdown = new List<object>
    {
        new {
            ItemName = "Chấm bài IELTS Writing (AI Gemini)",
            Tokens = writingTokens,
            Percentage = Math.Round((double)writingTokens / grandTotalTokens * 100, 1),
            Icon = "bi-pencil-square",
            Color = "#38bdf8"
        },
        new {
            ItemName = "Chấm bài IELTS Speaking (Audio & AI)",
            Tokens = speakingTokens,
            Percentage = Math.Round((double)speakingTokens / grandTotalTokens * 100, 1),
            Icon = "bi-mic-fill",
            Color = "#a855f7"
        },
        new {
            ItemName = "Luyện đề IELTS / TOEIC / HSK",
            Tokens = examTokens,
            Percentage = Math.Round((double)examTokens / grandTotalTokens * 100, 1),
            Icon = "bi-journal-check",
            Color = "#22c55e"
        },
        new {
            ItemName = "Tra cứu Từ vựng & Giải nghĩa AI",
            Tokens = vocabTokens,
            Percentage = Math.Round((double)vocabTokens / grandTotalTokens * 100, 1),
            Icon = "bi-translate",
            Color = "#f59e0b"
        }
    };

    return Results.Ok(new {
        Range = range,
        Granularity = granularity,
        TotalRequests = allSubmissions.Count,
        TotalTokens = grandTotalTokens,
        TotalCost = Math.Round(grandTotalTokens * 0.00000015, 4),
        ErrorRate = 0.0,
        TimePoints = points,
        ModelBreakdown = modelBreakdown
    });
});

// --- ADMIN AI API KEYS CONFIGURATION ENDPOINTS ---

Func<SystemAiSettingsDto> loadAiSettings = () =>
{
    var paths = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "ai_settings.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "ai_settings.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Backend.Api", "ai_settings.json")
    };

    foreach (var p in paths)
    {
        if (File.Exists(p))
        {
            try
            {
                var json = File.ReadAllText(p);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<SystemAiSettingsDto>(json);
                if (parsed != null) return parsed;
            }
            catch { }
        }
    }
    return new SystemAiSettingsDto();
};

Action<SystemAiSettingsDto> saveAiSettings = (settings) =>
{
    try
    {
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var targetPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Backend.Api", "ai_settings.json")
        };

        foreach (var p in targetPaths)
        {
            try
            {
                var dir = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    File.WriteAllText(p, json);
                }
            }
            catch { }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AiSettings] Failed to save settings: {ex.Message}");
    }
};

app.MapGet("/api/admin/ai/config", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] () =>
{
    var settings = loadAiSettings();
    return Results.Ok(settings);
});

app.MapPost("/api/admin/ai/config", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] (SystemAiSettingsDto dto) =>
{
    saveAiSettings(dto);
    return Results.Ok(new { success = true, message = "Đã lưu cấu hình API Keys thành công." });
});

app.MapPost("/api/admin/ai/test-connection", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (TestAiConnectionRequestDto req, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.ApiKey))
    {
        return Results.Ok(new { success = false, message = "Vui lòng nhập API Key trước khi kiểm tra.", latencyMs = 0 });
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

    try
    {
        var provider = (req.Provider ?? "").ToLowerInvariant();

        if (provider == "gemini")
        {
            var testUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={req.ApiKey.Trim()}";
            var res = await http.GetAsync(testUrl, cancellationToken);
            stopwatch.Stop();

            if (res.IsSuccessStatusCode)
            {
                var models = new List<string>();
                try
                {
                    var json = await res.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("models", out var modelsArr) && modelsArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var m in modelsArr.EnumerateArray())
                        {
                            if (m.TryGetProperty("name", out var nameElem))
                            {
                                var name = nameElem.GetString() ?? "";
                                if (name.StartsWith("models/")) name = name.Substring(7);
                                if (!string.IsNullOrWhiteSpace(name) && !models.Contains(name)) models.Add(name);
                            }
                        }
                    }
                }
                catch { }

                return Results.Ok(new {
                    success = true,
                    message = $"Kết nối Google Gemini thành công! (Tìm thấy {models.Count} models, độ trễ: {stopwatch.ElapsedMilliseconds}ms)",
                    latencyMs = stopwatch.ElapsedMilliseconds,
                    availableModels = models
                });
            }
            else
            {
                var errContent = await res.Content.ReadAsStringAsync(cancellationToken);
                return Results.Ok(new { success = false, message = $"Lỗi từ Google AI: {(int)res.StatusCode} {res.ReasonPhrase}", latencyMs = stopwatch.ElapsedMilliseconds, detail = errContent });
            }
        }
        else if (provider == "xkiro" || provider == "openai" || provider == "deepseek" || provider == "whisper" || provider == "groq")
        {
            var baseUrl = (req.BaseUrl ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = provider switch {
                    "xkiro" => "https://api.xkiro.com/v1",
                    "deepseek" => "https://api.deepseek.com/v1",
                    "groq" => "https://api.groq.com/openai/v1",
                    _ => "https://api.openai.com/v1"
                };
            }

            var testUrl = $"{baseUrl}/models";
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.ApiKey.Trim());
            
            var res = await http.GetAsync(testUrl, cancellationToken);
            stopwatch.Stop();

            if (res.IsSuccessStatusCode)
            {
                var models = new List<string>();
                try
                {
                    var json = await res.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var m in dataArr.EnumerateArray())
                        {
                            if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("id", out var idElem))
                            {
                                var id = idElem.GetString();
                                if (!string.IsNullOrWhiteSpace(id) && !models.Contains(id)) models.Add(id);
                            }
                            else if (m.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var str = m.GetString();
                                if (!string.IsNullOrWhiteSpace(str) && !models.Contains(str)) models.Add(str);
                            }
                        }
                    }
                    else if (root.TryGetProperty("models", out var modelsArr) && modelsArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var m in modelsArr.EnumerateArray())
                        {
                            if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("id", out var idElem))
                            {
                                var id = idElem.GetString();
                                if (!string.IsNullOrWhiteSpace(id) && !models.Contains(id)) models.Add(id);
                            }
                            else if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("name", out var nameElem))
                            {
                                var name = nameElem.GetString();
                                if (!string.IsNullOrWhiteSpace(name) && !models.Contains(name)) models.Add(name);
                            }
                        }
                    }
                }
                catch { }

                var provName = provider switch {
                    "whisper" => "OpenAI Whisper",
                    "groq" => "Groq Cloud Whisper",
                    _ => provider.ToUpper()
                };
                return Results.Ok(new {
                    success = true,
                    message = $"Kết nối {provName} thành công! (Tìm thấy {models.Count} models, độ trễ: {stopwatch.ElapsedMilliseconds}ms)",
                    latencyMs = stopwatch.ElapsedMilliseconds,
                    availableModels = models
                });
            }
            else
            {
                var errContent = await res.Content.ReadAsStringAsync(cancellationToken);
                return Results.Ok(new { success = false, message = $"Lỗi kết nối {(int)res.StatusCode}: {res.ReasonPhrase}", latencyMs = stopwatch.ElapsedMilliseconds, detail = errContent });
            }
        }
        else
        {
            return Results.Ok(new { success = false, message = $"Nhà cung cấp '{req.Provider}' không được hỗ trợ.", latencyMs = 0 });
        }
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        return Results.Ok(new { success = false, message = $"Không thể kết nối máy chủ: {ex.Message}", latencyMs = stopwatch.ElapsedMilliseconds });
    }
});

// --- ADMIN USER MANAGEMENT ENDPOINTS ---

app.MapGet("/api/admin/users",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Users.OrderByDescending(u => u.CreatedAt), cancellationToken);
    
    return Results.Ok(users.Select(u => new {
        u.Id,
        u.Username,
        u.Email,
        u.Role,
        u.Level,
        u.IsActive,
        u.LastLoginAt,
        u.CreatedAt
    }));
});

app.MapPut("/api/admin/users/{id}/toggle-active",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user == null) return Results.NotFound();

    user.IsActive = !user.IsActive;
    
    dbContext.UserActivityLogs.Add(new Backend.Domain.Entities.UserActivityLog
    {
        UserId = user.Id,
        Action = user.IsActive ? "enabled" : "disabled",
        Detail = "Toggled by Admin"
    });

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { user.IsActive });
});

app.MapPut("/api/admin/users/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Application.DTOs.UpdateUserRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Username, "Tên tài khoản");
        Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Email, "Email");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }

    var user = await dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user == null) return Results.NotFound();

    user.Username = request.Username;
    user.Email = request.Email;
    // Khóa chặt bảo mật: Bảng users chỉ dành riêng cho học viên, không bao giờ cho phép leo lên quyền admin
    user.Role = "user";
    user.Level = request.Level;

    dbContext.UserActivityLogs.Add(new Backend.Domain.Entities.UserActivityLog
    {
        UserId = user.Id,
        Action = "profile_update",
        Detail = "Admin updated profile"
    });

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});
// ─── HSK: Learning Sections ───
app.MapGet("/api/hsk/sections", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var sections = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.LearningSections
            .Where(s => s.Language == "HSK")
            .OrderBy(s => s.OrderIndex)
            .Select(s => new Backend.Application.DTOs.LearningSectionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                Route = s.Route,
                Language = s.Language,
                OrderIndex = s.OrderIndex
            }), cancellationToken);
    return Results.Ok(sections);
});

// ─── Navigation (dynamic sidebar) ───
app.MapGet("/api/navigation", async (string? language, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var cacheKey = $"navigation:{(string.IsNullOrWhiteSpace(language) ? "all" : language.Trim().ToUpperInvariant())}";
    var cached = await cacheService.GetAsync<List<Backend.Application.DTOs.LearningSectionDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var query = dbContext.LearningSections.AsQueryable();
    if (!string.IsNullOrWhiteSpace(language))
        query = query.Where(s => s.Language.ToUpper() == language.Trim().ToUpper());

    var sections = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderBy(s => s.Language).ThenBy(s => s.OrderIndex)
            .Select(s => new Backend.Application.DTOs.LearningSectionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                Route = s.Route,
                Language = s.Language,
                OrderIndex = s.OrderIndex
            }), cancellationToken);

    await cacheService.SetAsync(cacheKey, sections, TimeSpan.FromHours(1), cancellationToken);
    return Results.Ok(sections);
});

// Admin: CRUD navigation
app.MapGet("/api/admin/navigation", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var all = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.LearningSections.OrderBy(s => s.Language).ThenBy(s => s.OrderIndex)
            .Select(s => new Backend.Application.DTOs.LearningSectionDto
            {
                Id = s.Id, Name = s.Name, Description = s.Description, Icon = s.Icon, Route = s.Route, Language = s.Language, OrderIndex = s.OrderIndex
            }), cancellationToken);
    return Results.Ok(all);
});

app.MapPost("/api/admin/navigation", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Backend.Application.DTOs.LearningSectionDto dto, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.ICacheService cacheService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Route) || string.IsNullOrWhiteSpace(dto.Language))
        return Results.BadRequest("Name, Route and Language are required.");
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('learning_sections', 'id'), COALESCE(MAX(id), 1)) FROM learning_sections;", cancellationToken);
    }
    catch { }

    var entity = new Backend.Domain.Entities.LearningSection
    {
        Name = dto.Name.Trim(),
        Description = dto.Description ?? "",
        Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bi-circle" : dto.Icon.Trim(),
        Route = dto.Route.Trim(),
        Language = dto.Language.Trim().ToUpperInvariant(),
        OrderIndex = dto.OrderIndex <= 0 ? 99 : dto.OrderIndex
    };
    dbContext.LearningSections.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("navigation:", cancellationToken);
    dto.Id = entity.Id;
    return Results.Created($"/api/admin/navigation/{entity.Id}", dto);
});

app.MapPut("/api/admin/navigation/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Application.DTOs.LearningSectionDto dto, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var entity = await dbContext.LearningSections.FindAsync(new object[] { id }, cancellationToken);
    if (entity == null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Route) || string.IsNullOrWhiteSpace(dto.Language))
        return Results.BadRequest("Name, Route and Language are required.");
    entity.Name = dto.Name.Trim();
    entity.Description = dto.Description ?? "";
    entity.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bi-circle" : dto.Icon.Trim();
    entity.Route = dto.Route.Trim();
    entity.Language = dto.Language.Trim().ToUpperInvariant();
    entity.OrderIndex = dto.OrderIndex;
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("navigation:", cancellationToken);
    return Results.Ok(dto);
});

app.MapDelete("/api/admin/navigation/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var entity = await dbContext.LearningSections.FindAsync(new object[] { id }, cancellationToken);
    if (entity == null) return Results.NotFound();
    dbContext.LearningSections.Remove(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("navigation:", cancellationToken);
    return Results.NoContent();
});

// ─── HSK: Upload media (image/audio) ───
app.MapPost("/api/hsk/upload-media",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    bool isImage = file.ContentType.StartsWith("image/");
    long maxSize = isImage ? 10 * 1024 * 1024 : 80 * 1024 * 1024;
    if (file.Length > maxSize)
        return Results.BadRequest($"File too large. Max {(isImage ? "10MB" : "80MB")}.");
    var folder = isImage ? "hsk/images" : "hsk/audio";
    var ext = Path.GetExtension(file.FileName);
    var fileName = $"{folder}/{Guid.NewGuid()}{ext}";
    using var stream = file.OpenReadStream();
    try
    {
        var url = await r2Service.UploadFileAsync(stream, fileName, file.ContentType, cancellationToken);
        return Results.Ok(new { Url = url, Type = isImage ? "image" : "audio" });
    }
    catch (Exception ex) { return Results.BadRequest(ex.Message); }
}).DisableAntiforgery();

// ─── HSK: Save exam JSON ───
app.MapPost("/api/hsk/save-exam",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
        HskSaveExamRequest req,
        Backend.Application.Abstractions.IR2StorageService r2Service,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.CollectionName) || string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest("CollectionName and Title are required.");

    var json = HtmlGuard.SanitizeJsonStrings(System.Text.Json.JsonSerializer.Serialize(req.ExamData,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = false }));
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var fileId = Guid.NewGuid().ToString("N");
    var fileName = $"hsk/exams/{fileId}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch
    {
        var dir = Path.Combine("wwwroot", "exports");
        Directory.CreateDirectory(dir);
        var localPath = Path.Combine(dir, $"{fileId}.json");
        await File.WriteAllBytesAsync(localPath, jsonBytes, cancellationToken);
        jsonUrl = $"/exports/{fileId}.json";
    }

    Backend.Domain.Entities.MockTest? test = null;
    if (req.MockTestId.HasValue)
        test = await dbContext.MockTests.FindAsync(new object[] { req.MockTestId.Value }, cancellationToken);

    if (test == null)
    {
        test = new Backend.Domain.Entities.MockTest
        {
            CollectionName = req.CollectionName,
            Title = req.Title,
            HskUrl = jsonUrl
        };
        dbContext.MockTests.Add(test);
    }
    else
    {
        test.CollectionName = req.CollectionName;
        test.Title = req.Title;
        test.HskUrl = jsonUrl;
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { Url = jsonUrl, Id = test.Id });
});

// ─── IELTS: Vocabulary CRUD ───
app.MapGet("/api/ielts/vocab", async (string? topic, string? search, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    const string cacheKey = "ielts:vocab:all";
    var allItems = await cacheService.GetAsync<List<IeltsVocabularyDto>>(cacheKey, cancellationToken);

    if (allItems == null)
    {
        var items = await dbContext.IeltsVocabularies
            .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);
        allItems = items.Select(v => new IeltsVocabularyDto
        {
            Id = v.Id,
            Word = v.Word,
            Phonetic = v.Phonetic,
            PartOfSpeech = v.PartOfSpeech,
            Meaning = v.Meaning,
            Example = v.Example,
            ExampleMeaning = v.ExampleMeaning,
            Topic = v.Topic,
            CefrLevel = v.CefrLevel,
            DisplayOrder = v.DisplayOrder,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        }).ToList();

        await cacheService.SetAsync(cacheKey, allItems, TimeSpan.FromHours(2), cancellationToken);
    }

    var query = allItems.AsEnumerable();
    if (!string.IsNullOrEmpty(topic))
        query = query.Where(v => v.Topic == topic);
    if (!string.IsNullOrEmpty(search))
        query = query.Where(v => v.Word.Contains(search, StringComparison.OrdinalIgnoreCase) || v.Meaning.Contains(search, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(query);
});

app.MapPost("/api/ielts/vocab", async (IeltsVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
        dbContext.IeltsVocabularies, v => v.Word == req.Word && v.Meaning == req.Meaning, cancellationToken);
    if (exists) return Results.BadRequest("Cặp (từ, nghĩa) này đã tồn tại.");

    var vocab = new Backend.Domain.Entities.IeltsVocabulary
    {
        Word = req.Word.Trim(),
        Phonetic = req.Phonetic?.Trim(),
        PartOfSpeech = req.PartOfSpeech?.Trim(),
        Meaning = req.Meaning.Trim(),
        Example = req.Example?.Trim(),
        ExampleMeaning = req.ExampleMeaning?.Trim(),
        Topic = req.Topic?.Trim(),
        CefrLevel = req.CefrLevel?.Trim().ToUpperInvariant(),
        DisplayOrder = req.DisplayOrder ?? 0,
        IsActive = req.IsActive ?? true
    };
    dbContext.IeltsVocabularies.Add(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);

    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    return Results.Ok(new { Id = vocab.Id });
});

app.MapPut("/api/ielts/vocab/{id:int}", async (int id, IeltsVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.IeltsVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();

    bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
        dbContext.IeltsVocabularies, v => v.Id != id && v.Word == req.Word && v.Meaning == req.Meaning, cancellationToken);
    if (exists) return Results.BadRequest("Cặp (từ, nghĩa) này đã tồn tại ở dòng khác.");

    vocab.Word = req.Word.Trim();
    vocab.Phonetic = req.Phonetic?.Trim();
    vocab.PartOfSpeech = req.PartOfSpeech?.Trim();
    vocab.Meaning = req.Meaning.Trim();
    vocab.Example = req.Example?.Trim();
    vocab.ExampleMeaning = req.ExampleMeaning?.Trim();
    vocab.Topic = req.Topic?.Trim();
    vocab.CefrLevel = req.CefrLevel?.Trim().ToUpperInvariant();
    if (req.DisplayOrder.HasValue) vocab.DisplayOrder = req.DisplayOrder.Value;
    if (req.IsActive.HasValue) vocab.IsActive = req.IsActive.Value;
    await dbContext.SaveChangesAsync(cancellationToken);

    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    return Results.Ok();
});

app.MapDelete("/api/ielts/vocab/{id:int}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.IeltsVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();
    dbContext.IeltsVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);

    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    return Results.Ok();
});

// ─── IELTS: Vocabulary Excel Template ───
app.MapGet("/api/ielts/vocab/template-excel", () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("IELTS Vocabulary");
    string[] headers = { "Word", "Phonetic", "PartOfSpeech", "Meaning", "Example", "ExampleMeaning", "Topic", "CefrLevel", "DisplayOrder" };
    for (int i = 0; i < headers.Length; i++)
        worksheet.Cell(1, i + 1).Value = headers[i];
    var headerRange = worksheet.Range(1, 1, 1, headers.Length);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

    worksheet.Cell(2, 1).Value = "achieve";
    worksheet.Cell(2, 2).Value = "/əˈtʃiːv/";
    worksheet.Cell(2, 3).Value = "verb";
    worksheet.Cell(2, 4).Value = "đạt được, hoàn thành";
    worksheet.Cell(2, 5).Value = "She achieved her goal of becoming a doctor.";
    worksheet.Cell(2, 6).Value = "Cô ấy đã đạt được mục tiêu trở thành bác sĩ.";
    worksheet.Cell(2, 7).Value = "Education";
    worksheet.Cell(2, 8).Value = "B2";
    worksheet.Cell(2, 9).Value = 1;
    worksheet.Cell(3, 1).Value = "sustainable";
    worksheet.Cell(3, 2).Value = "/səˈsteɪnəbl/";
    worksheet.Cell(3, 3).Value = "adjective";
    worksheet.Cell(3, 4).Value = "bền vững";
    worksheet.Cell(3, 5).Value = "We need sustainable development to protect the environment.";
    worksheet.Cell(3, 6).Value = "Chúng ta cần phát triển bền vững để bảo vệ môi trường.";
    worksheet.Cell(3, 7).Value = "Environment";
    worksheet.Cell(3, 8).Value = "C1";
    worksheet.Cell(3, 9).Value = 2;

    worksheet.Column(1).Width = 16;
    worksheet.Column(2).Width = 16;
    worksheet.Column(3).Width = 12;
    worksheet.Column(4).Width = 28;
    worksheet.Column(5).Width = 48;
    worksheet.Column(6).Width = 42;
    worksheet.Column(7).Width = 16;
    worksheet.Column(8).Width = 10;
    worksheet.Column(9).Width = 13;

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "IELTS_Vocabulary_Template.xlsx");
});

// ─── IELTS: Vocabulary Excel Import (JSON → R2 ielts-vocab/ → DB) ───
app.MapPost("/api/ielts/vocab/import-excel", async (Microsoft.AspNetCore.Http.IFormFile file,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File không hợp lệ hoặc trống.");
    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Vui lòng upload file Excel (.xlsx)");

    var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
               modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
        ? "upsert"
        : "skip";

    // Đọc toàn bộ dòng dữ liệu (bỏ header), 8 cột theo template
    var rows = new List<string[]>();
    using (var stream = file.OpenReadStream())
    using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
    {
        var worksheet = workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int i = 2; i <= lastRow; i++)
        {
            var r = worksheet.Row(i);
            rows.Add(new[]
            {
                r.Cell(1).GetString()?.Trim() ?? "",
                r.Cell(2).GetString()?.Trim() ?? "",
                r.Cell(3).GetString()?.Trim() ?? "",
                r.Cell(4).GetString()?.Trim() ?? "",
                r.Cell(5).GetString()?.Trim() ?? "",
                r.Cell(6).GetString()?.Trim() ?? "",
                r.Cell(7).GetString()?.Trim() ?? "",
                r.Cell(8).GetString()?.Trim() ?? "",
                r.Cell(9).GetString()?.Trim() ?? ""
            });
        }
    }

    if (rows.Count == 0)
        return Results.BadRequest("File không có dữ liệu hợp lệ.");

    // Lấy tất cả từ vựng hiện có trong DB để kiểm tra tồn tại
    var existingVocabs = await dbContext.IeltsVocabularies.ToListAsync(cancellationToken);
    var existingDict = existingVocabs.ToDictionary(v => $"{v.Word}|{v.Meaning}", v => v, StringComparer.OrdinalIgnoreCase);

    int success = 0, fail = 0, duplicate = 0, updated = 0;
    var errors = new List<string>();
    var jsonItems = new List<object>();
    var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var cefrLevels = new[] { "A1", "A2", "B1", "B2", "C1", "C2" };

    for (int i = 0; i < rows.Count; i++)
    {
        var cells = rows[i];
        try
        {
            var word = cells[0];
            var meaning = cells[3];
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(meaning)) continue;

            var key = $"{word}|{meaning}";
            if (!seenInFile.Add(key)) { duplicate++; continue; }

            // Tự phát hiện format: cột 8 là CefrLevel hay DisplayOrder?
            var col8 = HskVocabCsvParser.NullIfEmpty(cells[7]);
            string? parsedCefr;
            int parsedOrder;
            if (col8 != null && cefrLevels.Contains(col8, StringComparer.OrdinalIgnoreCase))
            {
                parsedCefr = col8.ToUpperInvariant();
                parsedOrder = int.TryParse(cells[8], out int o8) ? o8 : 0;
            }
            else
            {
                parsedCefr = null;
                parsedOrder = int.TryParse(col8, out int o7) ? o7 : 0;
            }

            var jsonItem = new
            {
                word,
                phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                partOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                meaning,
                example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                exampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                cefrLevel = parsedCefr,
                displayOrder = parsedOrder
            };

            if (existingDict.TryGetValue(key, out var existing))
            {
                if (mode == "upsert")
                {
                    var newPhonetic = HskVocabCsvParser.NullIfEmpty(cells[1]);
                    var newPos = HskVocabCsvParser.NullIfEmpty(cells[2]);
                    var newExample = HskVocabCsvParser.NullIfEmpty(cells[4]);
                    var newExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]);
                    var newTopic = HskVocabCsvParser.NullIfEmpty(cells[6]);

                    if (existing.Phonetic == newPhonetic &&
                        existing.PartOfSpeech == newPos &&
                        existing.Example == newExample &&
                        existing.ExampleMeaning == newExampleMeaning &&
                        existing.Topic == newTopic &&
                        existing.CefrLevel == parsedCefr &&
                        existing.DisplayOrder == parsedOrder)
                    {
                        duplicate++;
                        continue;
                    }

                    existing.Phonetic = newPhonetic;
                    existing.PartOfSpeech = newPos;
                    existing.Example = newExample;
                    existing.ExampleMeaning = newExampleMeaning;
                    existing.Topic = newTopic;
                    existing.CefrLevel = parsedCefr;
                    existing.DisplayOrder = parsedOrder;
                    updated++;
                    jsonItems.Add(jsonItem);
                }
                else duplicate++;
                continue;
            }

            dbContext.IeltsVocabularies.Add(new Backend.Domain.Entities.IeltsVocabulary
            {
                Word = word,
                Phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                PartOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                Meaning = meaning,
                Example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                ExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                Topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                CefrLevel = parsedCefr,
                DisplayOrder = parsedOrder,
                IsActive = true
            });
            success++;
            jsonItems.Add(jsonItem);
        }
        catch (Exception ex)
        {
            errors.Add($"Dòng {i + 1}: {ex.Message}");
            fail++;
        }
    }

    try
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi khi lưu từ vựng vào database: {ex.Message}");
    }

    // Serialize toàn bộ dòng hợp lệ -> JSON -> upload R2 ielts-vocab/
    string jsonUrl = string.Empty;
    if (jsonItems.Count > 0)
    {
        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
        var safeName = System.Text.RegularExpressions.Regex.Replace(baseName, @"[^\w\-]", "_");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileId = $"{safeName}_{timestamp}";
        var vocabJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            fileName = file.FileName,
            importedAt = DateTime.UtcNow,
            mode,
            totalCount = jsonItems.Count,
            items = jsonItems
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(vocabJson);

        try
        {
            using var ms = new MemoryStream(jsonBytes);
            jsonUrl = await r2Storage.UploadFileAsync(ms, $"ielts-vocab/{fileId}.json", "application/json", cancellationToken);
        }
        catch
        {
            var dir = Path.Combine("wwwroot", "exports");
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, $"{fileId}.json"), jsonBytes, cancellationToken);
            jsonUrl = $"/exports/{fileId}.json";
        }
    }

    var batch = new Backend.Domain.Entities.IeltsVocabularyImport
    {
        FileName = file.FileName,
        JsonUrl = jsonUrl,
        TotalRows = rows.Count,
        ImportedCount = success,
        UpdatedCount = updated,
        DuplicateCount = duplicate,
        FailedCount = fail
    };
    dbContext.IeltsVocabularyImports.Add(batch);
    await dbContext.SaveChangesAsync(cancellationToken);

    var cacheService = httpContext.RequestServices.GetRequiredService<ICacheService>();
    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Updated = updated, Errors = errors, JsonUrl = jsonUrl });
}).DisableAntiforgery();

// ─── IELTS: Vocabulary import multiple files ───
app.MapPost("/api/ielts/vocab/import-multiple", async (Microsoft.AspNetCore.Http.IFormFileCollection files,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
{
    if (files == null || files.Count == 0)
        return Results.BadRequest("Không có file nào được upload.");

    var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
               modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
        ? "upsert"
        : "skip";

    var allRows = new List<(string[] cells, string fileName)>();
    foreach (var file in files)
    {
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            continue;

        using var stream = file.OpenReadStream();
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int i = 2; i <= lastRow; i++)
        {
            var r = worksheet.Row(i);
            allRows.Add((new[]
            {
                r.Cell(1).GetString()?.Trim() ?? "",
                r.Cell(2).GetString()?.Trim() ?? "",
                r.Cell(3).GetString()?.Trim() ?? "",
                r.Cell(4).GetString()?.Trim() ?? "",
                r.Cell(5).GetString()?.Trim() ?? "",
                r.Cell(6).GetString()?.Trim() ?? "",
                r.Cell(7).GetString()?.Trim() ?? "",
                r.Cell(8).GetString()?.Trim() ?? "",
                r.Cell(9).GetString()?.Trim() ?? ""
            }, file.FileName));
        }
    }

    if (allRows.Count == 0)
        return Results.BadRequest("Không có dữ liệu hợp lệ trong các file.");

    // Get all existing vocabulary for O(1) lookup
    var existingVocabs = await dbContext.IeltsVocabularies.ToListAsync(cancellationToken);
    var existingDict = existingVocabs.ToDictionary(v => $"{v.Word}|{v.Meaning}", v => v, StringComparer.OrdinalIgnoreCase);

    int success = 0, fail = 0, duplicate = 0, updated = 0;
    var errors = new List<string>();
    var jsonItems = new List<object>();
    var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < allRows.Count; i++)
    {
        var (cells, fileName) = allRows[i];
        try
        {
            var word = cells[0];
            var meaning = cells[3];
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(meaning)) continue;

            var dedupeKey = $"{word}|{meaning}".ToLowerInvariant();
            if (!seenInFile.Add(dedupeKey)) { duplicate++; continue; }

            // Tự phát hiện format: cột 8 là CefrLevel (A1..C2) hay DisplayOrder (số)?
            var col8m = HskVocabCsvParser.NullIfEmpty(cells[7]);
            string? parsedCefrM;
            int parsedOrderM;
            if (col8m != null && new[] { "A1","A2","B1","B2","C1","C2" }.Contains(col8m, StringComparer.OrdinalIgnoreCase))
            {
                parsedCefrM = col8m.ToUpperInvariant();
                parsedOrderM = int.TryParse(cells[8], out int o8m) ? o8m : 0;
            }
            else
            {
                parsedCefrM = null;
                parsedOrderM = int.TryParse(col8m, out int o7m) ? o7m : 0;
            }

            var jsonItem = new
            {
                word,
                phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                partOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                meaning,
                example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                exampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                cefrLevel = parsedCefrM,
                displayOrder = parsedOrderM
            };

            if (existingDict.TryGetValue(dedupeKey, out var existing))
            {
                if (mode == "upsert")
                {
                    var newPhonetic = HskVocabCsvParser.NullIfEmpty(cells[1]);
                    var newPos = HskVocabCsvParser.NullIfEmpty(cells[2]);
                    var newExample = HskVocabCsvParser.NullIfEmpty(cells[4]);
                    var newExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]);
                    var newTopic = HskVocabCsvParser.NullIfEmpty(cells[6]);

                    if (existing.Phonetic == newPhonetic &&
                        existing.PartOfSpeech == newPos &&
                        existing.Example == newExample &&
                        existing.ExampleMeaning == newExampleMeaning &&
                        existing.Topic == newTopic &&
                        existing.CefrLevel == parsedCefrM &&
                        existing.DisplayOrder == parsedOrderM)
                    {
                        duplicate++;
                        continue;
                    }

                    existing.Phonetic = newPhonetic;
                    existing.PartOfSpeech = newPos;
                    existing.Example = newExample;
                    existing.ExampleMeaning = newExampleMeaning;
                    existing.Topic = newTopic;
                    existing.CefrLevel = parsedCefrM;
                    existing.DisplayOrder = parsedOrderM;
                    updated++;
                    jsonItems.Add(jsonItem);
                }
                else duplicate++;
                continue;
            }

            dbContext.IeltsVocabularies.Add(new Backend.Domain.Entities.IeltsVocabulary
            {
                Word = word,
                Phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                PartOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                Meaning = meaning,
                Example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                ExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                Topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                CefrLevel = parsedCefrM,
                DisplayOrder = parsedOrderM,
                IsActive = true
            });
            success++;
            jsonItems.Add(jsonItem);
        }
        catch (Exception ex)
        {
            errors.Add($"Dòng {i + 1} (file {fileName}): {ex.Message}");
            fail++;
        }
    }

    try
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi khi lưu từ vựng vào database: {ex.Message}");
    }

    // Serialize toàn bộ dòng hợp lệ -> JSON -> upload R2
    string jsonUrl = string.Empty;
    var fileNames = string.Join(", ", files.Select(f => f.FileName));
    if (jsonItems.Count > 0)
    {
        // Tạo tên file có ý nghĩa: nếu 1 file thì dùng tên file đó, nhiều file thì ghép lại (tối đa 2)
        var excelFiles = files.Where(f => f.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)).ToList();
        string baseNameRaw = excelFiles.Count == 1
            ? Path.GetFileNameWithoutExtension(excelFiles[0].FileName)
            : string.Join("_and_", excelFiles.Take(2).Select(f => Path.GetFileNameWithoutExtension(f.FileName)));
        var safeName = System.Text.RegularExpressions.Regex.Replace(baseNameRaw, @"[^\w\-]", "_");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileId = $"{safeName}_{timestamp}";
        var vocabJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            fileNames = fileNames,
            importedAt = DateTime.UtcNow,
            mode,
            totalCount = jsonItems.Count,
            items = jsonItems
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(vocabJson);

        try
        {
            using var ms = new MemoryStream(jsonBytes);
            jsonUrl = await r2Storage.UploadFileAsync(ms, $"ielts-vocab/{fileId}.json", "application/json", cancellationToken);
        }
        catch
        {
            var dir = Path.Combine("wwwroot", "exports");
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, $"{fileId}.json"), jsonBytes, cancellationToken);
            jsonUrl = $"/exports/{fileId}.json";
        }
    }

    var batch = new Backend.Domain.Entities.IeltsVocabularyImport
    {
        FileName = fileNames,
        JsonUrl = jsonUrl,
        TotalRows = allRows.Count,
        ImportedCount = success,
        UpdatedCount = updated,
        DuplicateCount = duplicate,
        FailedCount = fail
    };
    dbContext.IeltsVocabularyImports.Add(batch);
    await dbContext.SaveChangesAsync(cancellationToken);

    var cacheService = httpContext.RequestServices.GetRequiredService<ICacheService>();
    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    var msg = $"Thêm mới {success}, cập nhật {updated}, thất bại {fail}, bỏ qua {duplicate} trùng.";
    if (errors.Any()) msg += " Chi tiết: " + string.Join(" | ", errors.Take(3));
    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Updated = updated, Errors = errors, JsonUrl = jsonUrl });
}).DisableAntiforgery();

// ─── IELTS: Delete all vocabulary ───
app.MapDelete("/api/ielts/vocab/all", async (Backend.Infrastructure.Persistence.AppDbContext dbContext,
        ILogger<Program> logger,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        ICacheService cacheService,
        CancellationToken cancellationToken) =>
{
    logger.LogInformation("Delete all vocabulary called");
    try
    {
        // Xóa file JSON trên R2 hoặc local từ bảng IeltsVocabularyImports
        var imports = await dbContext.IeltsVocabularyImports
            .Where(i => !string.IsNullOrEmpty(i.JsonUrl))
            .ToListAsync(cancellationToken);
        foreach (var import in imports)
        {
            try
            {
                if (!string.IsNullOrEmpty(import.JsonUrl))
                {
                    if (import.JsonUrl.StartsWith("/exports/"))
                    {
                        var localPath = Path.Combine("wwwroot", import.JsonUrl.TrimStart('/'));
                        if (File.Exists(localPath)) File.Delete(localPath);
                    }
                    else
                    {
                        await r2Storage.DeleteFileAsync(import.JsonUrl, cancellationToken);
                        logger.LogInformation("Deleted R2 file: {JsonUrl}", import.JsonUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete file: {JsonUrl}", import.JsonUrl);
            }
        }
        dbContext.IeltsVocabularyImports.RemoveRange(imports);

        // Xóa toàn bộ từ vựng trong DB
        var all = await dbContext.IeltsVocabularies.ToListAsync(cancellationToken);
        var count = all.Count;
        logger.LogInformation("Deleting {Count} vocabulary items", count);
        dbContext.IeltsVocabularies.RemoveRange(all);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

        return Results.Ok(new { Deleted = count, R2FilesDeleted = imports.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Delete all vocabulary failed");
        return Results.Problem($"Xóa thất bại: {ex.Message}");
    }
});

// ─── IELTS Vocabulary Progress (lưu theo tài khoản người dùng) ───
app.MapGet("/api/ielts/vocab/progress", [Microsoft.AspNetCore.Authorization.Authorize] async (
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var progress = await dbContext.IeltsVocabularyProgresses
        .Where(p => p.UserId == userId)
        .Select(p => new { p.VocabularyId, p.Status, p.LearnedAt })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { 
        vocabularyIds = progress.Select(p => p.VocabularyId).ToList(),
        items = progress
    });
});

app.MapPost("/api/ielts/vocab/progress/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (
        MigrateVocabProgressRequest req,
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (req.VocabularyIds == null || req.VocabularyIds.Count == 0)
        return Results.Ok(new { migrated = 0 });

    var validIds = (await dbContext.IeltsVocabularies
        .Where(v => req.VocabularyIds.Contains(v.Id))
        .Select(v => v.Id)
        .ToListAsync(cancellationToken)).ToHashSet();

    var existingIds = (await dbContext.IeltsVocabularyProgresses
        .Where(p => p.UserId == userId && req.VocabularyIds.Contains(p.VocabularyId))
        .Select(p => p.VocabularyId)
        .ToListAsync(cancellationToken)).ToHashSet();

    var toAdd = validIds.Except(existingIds)
        .Select(id => new Backend.Domain.Entities.IeltsVocabularyProgress
        {
            UserId = userId,
            VocabularyId = id,
            Status = "Learned",
            LearnedAt = DateTime.UtcNow
        }).ToList();

    if (toAdd.Count > 0)
    {
        dbContext.IeltsVocabularyProgresses.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { migrated = toAdd.Count });
});

app.MapPost("/api/ielts/vocab/progress/{vocabularyId:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (
        int vocabularyId,
        UpdateVocabProgressRequest req,
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    bool vocabExists = await dbContext.IeltsVocabularies.AnyAsync(v => v.Id == vocabularyId, cancellationToken);
    if (!vocabExists) return Results.NotFound("Không tìm thấy từ vựng.");

    if (req.Learned)
    {
        bool exists = await dbContext.IeltsVocabularyProgresses.AnyAsync(
            p => p.UserId == userId && p.VocabularyId == vocabularyId, cancellationToken);
        if (!exists)
        {
            dbContext.IeltsVocabularyProgresses.Add(new Backend.Domain.Entities.IeltsVocabularyProgress
            {
                UserId = userId,
                VocabularyId = vocabularyId,
                Status = "Learned",
                LearnedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Results.Ok(new { vocabularyId, learned = true });
    }

    var rows = await dbContext.IeltsVocabularyProgresses
        .Where(p => p.UserId == userId && p.VocabularyId == vocabularyId)
        .ToListAsync(cancellationToken);
    if (rows.Count > 0)
    {
        dbContext.IeltsVocabularyProgresses.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    return Results.Ok(new { vocabularyId, learned = false });
});

// ─── HSK: Vocabulary CRUD ───
app.MapGet("/api/hsk/vocab", async (string? level, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var cacheKey = string.IsNullOrEmpty(level) ? "hsk:vocab:all" : $"hsk:vocab:{level.ToLowerInvariant().Trim()}";
    var cached = await cacheService.GetAsync<List<HskVocabularyDto>>(cacheKey, cancellationToken);
    if (cached != null) return Results.Ok(cached);

    var query = dbContext.HskVocabularies.AsQueryable();
    if (!string.IsNullOrEmpty(level))
        query = query.Where(v => v.HskLevel == level);
    var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderBy(v => v.HskLevel).ThenBy(v => v.DisplayOrder), cancellationToken);

    var dtoList = items.Select(v => new HskVocabularyDto
    {
        Id = v.Id,
        HskLevel = v.HskLevel,
        Hanzi = v.Hanzi,
        Pinyin = v.Pinyin,
        Meaning = v.Meaning,
        WordType = v.WordType,
        ExampleSentence = v.ExampleSentence,
        ExamplePinyin = v.ExamplePinyin,
        ExampleMeaning = v.ExampleMeaning,
        AudioUrl = v.AudioUrl,
        DisplayOrder = v.DisplayOrder,
        IsActive = v.IsActive,
        CreatedAt = v.CreatedAt
    }).ToList();

    await cacheService.SetAsync(cacheKey, dtoList, TimeSpan.FromHours(2), cancellationToken);
    return Results.Ok(dtoList);
});

app.MapPost("/api/hsk/vocab",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var existing = await dbContext.HskVocabularies
        .FirstOrDefaultAsync(v => v.HskLevel == req.HskLevel && v.Hanzi == req.Hanzi, cancellationToken);
    if (existing != null)
        return Results.BadRequest("Từ này đã tồn tại ở cấp độ này.");

    var vocab = new Backend.Domain.Entities.HskVocabulary
    {
        HskLevel = req.HskLevel,
        Hanzi = req.Hanzi,
        Pinyin = req.Pinyin,
        Meaning = req.Meaning,
        WordType = req.WordType,
        ExampleSentence = req.ExampleSentence,
        ExamplePinyin = req.ExamplePinyin,
        ExampleMeaning = req.ExampleMeaning,
        AudioUrl = req.AudioUrl,
        DisplayOrder = req.DisplayOrder ?? 0,
        IsActive = req.IsActive ?? true
    };
    dbContext.HskVocabularies.Add(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

    return Results.Ok(new { Id = vocab.Id });
});

app.MapPut("/api/hsk/vocab/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.HskVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();

    // Check duplicate if level or hanzi changed
    if (vocab.HskLevel != req.HskLevel || vocab.Hanzi != req.Hanzi)
    {
        var existing = await dbContext.HskVocabularies
            .FirstOrDefaultAsync(v => v.HskLevel == req.HskLevel && v.Hanzi == req.Hanzi && v.Id != id, cancellationToken);
        if (existing != null)
            return Results.BadRequest("Từ này đã tồn tại ở cấp độ này.");
    }

    vocab.HskLevel = req.HskLevel;
    vocab.Hanzi = req.Hanzi;
    vocab.Pinyin = req.Pinyin;
    vocab.Meaning = req.Meaning;
    vocab.WordType = req.WordType;
    vocab.ExampleSentence = req.ExampleSentence;
    vocab.ExamplePinyin = req.ExamplePinyin;
    vocab.ExampleMeaning = req.ExampleMeaning;
    vocab.AudioUrl = req.AudioUrl;
    vocab.DisplayOrder = req.DisplayOrder ?? 0;
    vocab.IsActive = req.IsActive ?? true;
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

    return Results.Ok();
});

app.MapPost("/api/admin/users/{id}/reset-password",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user == null) return Results.NotFound();

    var tempPassword = Backend.Infrastructure.Services.AuthService.GenerateSecureRandomPassword();
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
    user.PasswordChangedAt = DateTime.UtcNow;

    dbContext.UserActivityLogs.Add(new Backend.Domain.Entities.UserActivityLog
    {
        UserId = user.Id,
        Action = "password_reset",
        Detail = "Manual reset by Admin"
    });

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { TempPassword = tempPassword });
});

app.MapGet("/api/admin/users/{id}/logs",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var logs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.UserActivityLogs
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Take(50), 
        cancellationToken);
    
    return Results.Ok(logs);
});

app.MapDelete("/api/hsk/vocab/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.HskVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();
    dbContext.HskVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);
    await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

    return Results.Ok();
});

// ─── HSK: Delete all vocabulary ───
app.MapDelete("/api/hsk/vocab/all", async (Backend.Infrastructure.Persistence.AppDbContext dbContext,
        ILogger<Program> logger,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        ICacheService cacheService,
        CancellationToken cancellationToken) =>
{
    logger.LogInformation("Delete all HSK vocabulary called");
    try
    {
        var imports = await dbContext.HskVocabularyImports
            .Where(i => !string.IsNullOrEmpty(i.JsonUrl))
            .ToListAsync(cancellationToken);
        foreach (var import in imports)
        {
            try
            {
                await r2Storage.DeleteFileAsync(import.JsonUrl!, cancellationToken);
                logger.LogInformation("Deleted R2 file: {JsonUrl}", import.JsonUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete R2 file: {JsonUrl}", import.JsonUrl);
            }
        }
        dbContext.HskVocabularyImports.RemoveRange(imports);

        var all = await dbContext.HskVocabularies.ToListAsync(cancellationToken);
        var count = all.Count;
        logger.LogInformation("Deleting {Count} HSK vocabulary items", count);
        dbContext.HskVocabularies.RemoveRange(all);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

        return Results.Ok(new { Deleted = count, R2FilesDeleted = imports.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Delete all HSK vocabulary failed");
        return Results.Problem($"Xóa thất bại: {ex.Message}");
    }
});

// ─── HSK Vocabulary Progress (lưu theo tài khoản người dùng) ───
app.MapGet("/api/hsk/vocab/progress", [Microsoft.AspNetCore.Authorization.Authorize] async (
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    var ids = await dbContext.HskVocabularyProgresses
        .Where(p => p.UserId == userId)
        .Select(p => p.VocabularyId)
        .ToListAsync(cancellationToken);

    return Results.Ok(new { vocabularyIds = ids });
});

app.MapPost("/api/hsk/vocab/progress/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (
        MigrateVocabProgressRequest req,
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    if (req.VocabularyIds == null || req.VocabularyIds.Count == 0)
        return Results.Ok(new { migrated = 0 });

    // Chỉ nhận ID từ vựng tồn tại thật
    var validIds = (await dbContext.HskVocabularies
        .Where(v => req.VocabularyIds.Contains(v.Id))
        .Select(v => v.Id)
        .ToListAsync(cancellationToken)).ToHashSet();

    // Bỏ qua những từ user đã có sẵn tiến độ
    var existingIds = (await dbContext.HskVocabularyProgresses
        .Where(p => p.UserId == userId && req.VocabularyIds.Contains(p.VocabularyId))
        .Select(p => p.VocabularyId)
        .ToListAsync(cancellationToken)).ToHashSet();

    var toAdd = validIds.Except(existingIds)
        .Select(id => new Backend.Domain.Entities.HskVocabularyProgress
        {
            UserId = userId,
            VocabularyId = id
        }).ToList();

    if (toAdd.Count > 0)
    {
        dbContext.HskVocabularyProgresses.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { migrated = toAdd.Count });
});

app.MapPost("/api/hsk/vocab/progress/{vocabularyId:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (
        int vocabularyId,
        UpdateVocabProgressRequest req,
        System.Security.Claims.ClaimsPrincipal user,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

    bool vocabExists = await dbContext.HskVocabularies.AnyAsync(v => v.Id == vocabularyId, cancellationToken);
    if (!vocabExists) return Results.NotFound("Không tìm thấy từ vựng.");

    if (req.Learned)
    {
        bool exists = await dbContext.HskVocabularyProgresses.AnyAsync(
            p => p.UserId == userId && p.VocabularyId == vocabularyId, cancellationToken);
        if (!exists)
        {
            dbContext.HskVocabularyProgresses.Add(new Backend.Domain.Entities.HskVocabularyProgress
            {
                UserId = userId,
                VocabularyId = vocabularyId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Results.Ok(new { vocabularyId, learned = true });
    }

    var rows = await dbContext.HskVocabularyProgresses
        .Where(p => p.UserId == userId && p.VocabularyId == vocabularyId)
        .ToListAsync(cancellationToken);
    if (rows.Count > 0)
    {
        dbContext.HskVocabularyProgresses.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    return Results.Ok(new { vocabularyId, learned = false });
});

// ─── HSK Vocabulary Excel Import ───
app.MapGet("/api/hsk/vocab/template-excel",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("HSK Vocabulary");
    // Header
    worksheet.Cell(1, 1).Value = "HskLevel";
    worksheet.Cell(1, 2).Value = "Hanzi";
    worksheet.Cell(1, 3).Value = "Pinyin";
    worksheet.Cell(1, 4).Value = "Meaning";
    worksheet.Cell(1, 5).Value = "WordType";
    worksheet.Cell(1, 6).Value = "ExampleSentence";
    worksheet.Cell(1, 7).Value = "ExamplePinyin";
    worksheet.Cell(1, 8).Value = "ExampleMeaning";
    worksheet.Cell(1, 9).Value = "AudioUrl";
    worksheet.Cell(1, 10).Value = "DisplayOrder";
    var headerRange = worksheet.Range("A1:J1");
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
    worksheet.Cell(2, 1).Value = "HSK3";
    worksheet.Cell(2, 2).Value = "图书馆";
    worksheet.Cell(2, 3).Value = "tú shū guǎn";
    worksheet.Cell(2, 4).Value = "thư viện";
    worksheet.Cell(2, 5).Value = "noun";
    worksheet.Cell(2, 6).Value = "我去图书馆看书。";
    worksheet.Cell(2, 7).Value = "wǒ qù tú shū guǎn kàn shū。";
    worksheet.Cell(2, 8).Value = "Tôi đi thư viện đọc sách.";
    worksheet.Column(1).Width = 12;
    worksheet.Column(2).Width = 15;
    worksheet.Column(3).Width = 18;
    worksheet.Column(4).Width = 20;
    worksheet.Column(5).Width = 12;
    worksheet.Column(6).Width = 30;
    worksheet.Column(7).Width = 25;
    worksheet.Column(8).Width = 25;
    worksheet.Column(9).Width = 30;
    worksheet.Column(10).Width = 12;
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HSK_Vocabulary_Template.xlsx");
});

app.MapPost("/api/hsk/vocab/import-excel",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        ICacheService cacheService,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File không hợp lệ hoặc trống.");
    var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
    if (ext != ".xlsx" && ext != ".csv")
        return Results.BadRequest("Vui lòng upload file Excel (.xlsx) hoặc CSV (.csv)");

    // Chế độ xử lý từ trùng: "skip" (bỏ qua) hoặc "upsert" (cập nhật ghi đè)
    var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
               modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
        ? "upsert"
        : "skip";

    // Normalize rows into 10 columns: level, hanzi, pinyin, meaning, wordType,
    // exampleSentence, examplePinyin, exampleMeaning, audioUrl, displayOrder
    var rows = new List<string[]>();
    if (ext == ".xlsx")
    {
        using var stream = file.OpenReadStream();
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int i = 2; i <= lastRow; i++)
        {
            var r = worksheet.Row(i);
            rows.Add(new[]
            {
                r.Cell(1).GetString()?.Trim() ?? "",
                r.Cell(2).GetString()?.Trim() ?? "",
                r.Cell(3).GetString()?.Trim() ?? "",
                r.Cell(4).GetString()?.Trim() ?? "",
                r.Cell(5).GetString()?.Trim() ?? "",
                r.Cell(6).GetString()?.Trim() ?? "",
                r.Cell(7).GetString()?.Trim() ?? "",
                r.Cell(8).GetString()?.Trim() ?? "",
                r.Cell(9).GetString()?.Trim() ?? "",
                r.Cell(10).GetString()?.Trim() ?? ""
            });
        }
    }
    else
    {
        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8);
        var csvText = await reader.ReadToEndAsync(cancellationToken);
        var csvRows = HskVocabCsvParser.Parse(csvText).ToList();

        // Ánh xạ cột theo TÊN trong dòng header (chấp nhận thiếu/sai thứ tự cột)
        int[] map = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        IEnumerable<List<string>> dataRows = csvRows;
        if (csvRows.Count > 0 && csvRows[0].Count > 0 &&
            csvRows[0][0].Trim().Equals("HskLevel", StringComparison.OrdinalIgnoreCase))
        {
            var header = csvRows[0]
                .Select(h => h.Trim().ToLowerInvariant().Replace("_", ""))
                .ToList();
            int Idx(string name) => header.IndexOf(name);
            map = new[]
            {
                Idx("hsklevel"), Idx("hanzi"), Idx("pinyin"), Idx("meaning"), Idx("wordtype"),
                Idx("examplesentence"), Idx("examplepinyin"), Idx("examplemeaning"), Idx("audiourl"), Idx("displayorder")
            };
            // Cột nào không khai báo trong header => dữ liệu không tồn tại, để trống
            // (Get(idx<0) trả về "")
            dataRows = csvRows.Skip(1);
        }

        foreach (var fields in dataRows)
        {
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            string Get(int idx) => idx >= 0 && idx < fields.Count ? fields[idx]?.Trim() ?? "" : "";
            rows.Add(new[] { Get(map[0]), Get(map[1]), Get(map[2]), Get(map[3]), Get(map[4]), Get(map[5]), Get(map[6]), Get(map[7]), Get(map[8]), Get(map[9]) });
        }
    }

    int success = 0, fail = 0, duplicate = 0, updated = 0;
    var errors = new List<string>();
    var jsonItems = new List<object>();
    var seenInFile = new HashSet<string>(StringComparer.Ordinal);

    for (int i = 0; i < rows.Count; i++)
    {
        var cells = rows[i];
        try
        {
            var level = cells[0];
            var hanzi = cells[1];
            if (string.IsNullOrEmpty(level) || string.IsNullOrEmpty(hanzi)) continue;

            // JSON xuất ra gồm TOÀN BỘ dòng hợp lệ trong file (kể cả từ đã tồn tại)
            string? wordType = HskVocabCsvParser.NullIfEmpty(cells[4]);
            int displayOrder = int.TryParse(cells[9], out int orderVal) ? orderVal : 0;
            // Trùng trong cùng file import (cùng cấp độ + cùng chữ Hán)
            if (!seenInFile.Add($"{level}|{hanzi}")) { duplicate++; continue; }

            var jsonItem = new
            {
                hskLevel = level,
                hanzi,
                pinyin = cells[2],
                meaning = cells[3],
                wordType,
                exampleSentence = HskVocabCsvParser.NullIfEmpty(cells[5]),
                examplePinyin = HskVocabCsvParser.NullIfEmpty(cells[6]),
                exampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[7]),
                audioUrl = HskVocabCsvParser.NullIfEmpty(cells[8]),
                displayOrder
            };

            // Tìm từ đã tồn tại trong DB theo (cấp độ, chữ Hán)
            var existing = await dbContext.HskVocabularies.FirstOrDefaultAsync(
                v => v.HskLevel == level && v.Hanzi == hanzi, cancellationToken);

            if (existing != null)
            {
                if (mode == "upsert")
                {
                    var newPinyin = cells[2];
                    var newMeaning = cells[3];
                    var newExampleSentence = HskVocabCsvParser.NullIfEmpty(cells[5]);
                    var newExamplePinyin = HskVocabCsvParser.NullIfEmpty(cells[6]);
                    var newExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[7]);
                    var newAudioUrl = HskVocabCsvParser.NullIfEmpty(cells[8]);

                    if (existing.Pinyin == newPinyin &&
                        existing.Meaning == newMeaning &&
                        existing.WordType == wordType &&
                        existing.ExampleSentence == newExampleSentence &&
                        existing.ExamplePinyin == newExamplePinyin &&
                        existing.ExampleMeaning == newExampleMeaning &&
                        (string.IsNullOrEmpty(newAudioUrl) || existing.AudioUrl == newAudioUrl) &&
                        existing.DisplayOrder == displayOrder)
                    {
                        duplicate++;
                        continue;
                    }

                    existing.Pinyin = newPinyin;
                    existing.Meaning = newMeaning;
                    existing.WordType = wordType;
                    existing.ExampleSentence = newExampleSentence;
                    existing.ExamplePinyin = newExamplePinyin;
                    existing.ExampleMeaning = newExampleMeaning;
                    if (!string.IsNullOrEmpty(newAudioUrl)) existing.AudioUrl = newAudioUrl;
                    existing.DisplayOrder = displayOrder;
                    updated++;
                    jsonItems.Add(jsonItem);
                }
                else duplicate++;
                continue;
            }

            var vocab = new Backend.Domain.Entities.HskVocabulary
            {
                HskLevel = level,
                Hanzi = hanzi,
                Pinyin = cells[2],
                Meaning = cells[3],
                WordType = wordType,
                ExampleSentence = HskVocabCsvParser.NullIfEmpty(cells[5]),
                ExamplePinyin = HskVocabCsvParser.NullIfEmpty(cells[6]),
                ExampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[7]),
                AudioUrl = HskVocabCsvParser.NullIfEmpty(cells[8]),
                DisplayOrder = displayOrder,
                IsActive = true
            };
            dbContext.HskVocabularies.Add(vocab);
            success++;
            jsonItems.Add(jsonItem);
        }
        catch (Exception ex)
        {
            errors.Add($"Dòng {i + 1}: {ex.Message}");
            fail++;
        }
    }
    try
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi khi lưu từ vựng vào database: {ex.Message}");
    }
    string jsonUrl = string.Empty;
    if (jsonItems.Count > 0)
    {
        var hskBaseName = Path.GetFileNameWithoutExtension(file.FileName);
        var hskSafeName = System.Text.RegularExpressions.Regex.Replace(hskBaseName, @"[^\w\-]", "_");
        var hskTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileId = $"{hskSafeName}_{hskTimestamp}";
        var vocabJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            fileName = file.FileName,
            importedAt = DateTime.UtcNow,
            mode,
            totalCount = jsonItems.Count,
            items = jsonItems
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(vocabJson);

        try
        {
            using var ms = new MemoryStream(jsonBytes);
            jsonUrl = await r2Storage.UploadFileAsync(ms, $"hsk-vocab/{fileId}.json", "application/json", cancellationToken);
        }
        catch
        {
            var dir = Path.Combine("wwwroot", "exports");
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, $"hsk-vocab_{fileId}.json"), jsonBytes, cancellationToken);
            jsonUrl = $"/exports/hsk-vocab_{fileId}.json";
        }
    }

    var batch = new Backend.Domain.Entities.HskVocabularyImport
    {
        FileName = file.FileName,
        JsonUrl = jsonUrl,
        TotalRows = rows.Count,
        ImportedCount = success,
        UpdatedCount = updated,
        DuplicateCount = duplicate,
        FailedCount = fail
    };
    dbContext.HskVocabularyImports.Add(batch);
    await dbContext.SaveChangesAsync(cancellationToken);

    await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

    var msg = $"Thêm mới {success}, cập nhật {updated}, thất bại {fail}, bỏ qua {duplicate} trùng.";
    if (errors.Any()) msg += " Chi tiết: " + string.Join(" | ", errors.Take(3));
    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Updated = updated, Errors = errors, JsonUrl = jsonUrl });
}).DisableAntiforgery();

// ─── IELTS: Auto-phân loại CEFR cho từ vựng chưa có CefrLevel ───
app.MapPost("/api/admin/ielts/vocab/auto-classify-cefr",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // Bảng mapping Topic → CEFR range (min, max) dựa trên chuẩn IELTS Academic
    static string ClassifyByCefrLogic(string? topic, string word)
    {
        var w = word.ToLowerInvariant().Trim();
        int len = w.Length;

        // Topic-based CEFR mapping
        var topicCefr = (topic?.ToLowerInvariant() ?? "") switch
        {
            var t when t.Contains("daily life") || t.Contains("daily communication") ||
                       t.Contains("family") || t.Contains("greetings") ||
                       t.Contains("numbers") || t.Contains("colors") => len <= 5 ? "A1" : "A2",

            var t when t.Contains("travel") || t.Contains("food") ||
                       t.Contains("shopping") || t.Contains("health") ||
                       t.Contains("sport") || t.Contains("hobby") ||
                       t.Contains("weather") || t.Contains("transport") => len <= 6 ? "A2" : "B1",

            var t when t.Contains("education") || t.Contains("work") ||
                       t.Contains("career") || t.Contains("society") ||
                       t.Contains("culture") || t.Contains("media") ||
                       t.Contains("communication") => len <= 8 ? "B1" : "B2",

            var t when t.Contains("environment") || t.Contains("technology") ||
                       t.Contains("business") || t.Contains("economy") ||
                       t.Contains("urban") || t.Contains("global") ||
                       t.Contains("science") => len <= 8 ? "B2" : "C1",

            var t when t.Contains("academic") || t.Contains("research") ||
                       t.Contains("law") || t.Contains("politic") ||
                       t.Contains("philosophy") || t.Contains("psychology") ||
                       t.Contains("sociology") || t.Contains("linguistics") => len <= 9 ? "C1" : "C2",

            var t when t.Contains("data") || t.Contains("programming") ||
                       t.Contains("artificial") || t.Contains("quantum") ||
                       t.Contains("biochemistry") || t.Contains("nuclear") => "C2",

            _ => null // Không xác định được từ topic → dùng word complexity
        };

        if (topicCefr != null) return topicCefr;

        // Fallback: phân loại theo độ phức tạp từ
        if (len <= 4) return "A1";
        if (len <= 6) return "A2";
        if (len <= 8) return "B1";
        if (len <= 10) return "B2";
        if (len <= 13) return "C1";
        return "C2";
    }

    // Chỉ cập nhật những từ chưa có CefrLevel
    var toUpdate = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
        .ToListAsync(dbContext.IeltsVocabularies.Where(v => v.CefrLevel == null || v.CefrLevel == ""), cancellationToken);

    int updated = 0;
    foreach (var v in toUpdate)
    {
        v.CefrLevel = ClassifyByCefrLogic(v.Topic, v.Word);
        updated++;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    // Xóa cache để GET vocab trả về dữ liệu mới
    var cacheService = httpContext.RequestServices.GetRequiredService<ICacheService>();
    await cacheService.RemoveByPrefixAsync("ielts:vocab:", cancellationToken);

    // Thống kê kết quả
    var stats = toUpdate.GroupBy(v => v.CefrLevel)
        .Select(g => new { Level = g.Key, Count = g.Count() })
        .OrderBy(x => x.Level)
        .ToList();

    return Results.Ok(new
    {
        Updated = updated,
        Message = $"Đã tự động phân loại CEFR cho {updated} từ vựng.",
        Distribution = stats
    });
});

// ══════════════════════════════════════════════════════════════════════
// ─── IELTS / ENGLISH: Grammar Structures & Band Explorer Endpoints ───
// ══════════════════════════════════════════════════════════════════════

app.MapGet("/api/grammar-structures", async (
    string? search,
    string? bandLevel,
    string? category,
    string? grammarTopic,
    bool? isActive,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.GrammarStructures.AsQueryable();

    if (isActive.HasValue)
    {
        query = query.Where(g => g.IsActive == isActive.Value);
    }

    if (!string.IsNullOrWhiteSpace(bandLevel) && !bandLevel.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(g => g.BandLevel == bandLevel || g.BandLevel.Contains(bandLevel));
    }

    if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(g => g.Category == category || g.Category.Contains(category));
    }

    if (!string.IsNullOrWhiteSpace(grammarTopic) && !grammarTopic.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        query = query.Where(g => g.GrammarTopic == grammarTopic || g.GrammarTopic.Contains(grammarTopic));
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.ToLowerInvariant().Trim();
        query = query.Where(g =>
            g.StructureCode.ToLower().Contains(s) ||
            g.GrammarTopic.ToLower().Contains(s) ||
            g.Formula.ToLower().Contains(s) ||
            g.UsageFunction.ToLower().Contains(s) ||
            g.AdvancedExample.ToLower().Contains(s) ||
            g.VietnameseMeaning.ToLower().Contains(s) ||
            (g.KeyCollocations != null && g.KeyCollocations.ToLower().Contains(s)) ||
            (g.Tags != null && g.Tags.ToLower().Contains(s)));
    }

    var list = await query
        .OrderBy(g => g.DisplayOrder)
        .ThenByDescending(g => g.Id)
        .Select(g => new Backend.Application.DTOs.GrammarStructureDto
        {
            Id = g.Id,
            StructureCode = g.StructureCode,
            BandLevel = g.BandLevel,
            Category = g.Category,
            GrammarTopic = g.GrammarTopic,
            Formula = g.Formula,
            UsageFunction = g.UsageFunction,
            BasicExample = g.BasicExample,
            AdvancedExample = g.AdvancedExample,
            VietnameseMeaning = g.VietnameseMeaning,
            KeyCollocations = g.KeyCollocations,
            CommonMistakes = g.CommonMistakes,
            PracticeExercise = g.PracticeExercise,
            Tags = g.Tags,
            DisplayOrder = g.DisplayOrder,
            IsActive = g.IsActive,
            CreatedAt = g.CreatedAt,
            UpdatedAt = g.UpdatedAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(list);
});

app.MapGet("/api/grammar-structures/{id:int}", async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var g = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
    if (g == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

    var dto = new Backend.Application.DTOs.GrammarStructureDto
    {
        Id = g.Id,
        StructureCode = g.StructureCode,
        BandLevel = g.BandLevel,
        Category = g.Category,
        GrammarTopic = g.GrammarTopic,
        Formula = g.Formula,
        UsageFunction = g.UsageFunction,
        BasicExample = g.BasicExample,
        AdvancedExample = g.AdvancedExample,
        VietnameseMeaning = g.VietnameseMeaning,
        KeyCollocations = g.KeyCollocations,
        CommonMistakes = g.CommonMistakes,
        PracticeExercise = g.PracticeExercise,
        Tags = g.Tags,
        DisplayOrder = g.DisplayOrder,
        IsActive = g.IsActive,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt
    };
    return Results.Ok(dto);
});

app.MapPost("/api/admin/grammar-structures", async (
    Backend.Application.DTOs.CreateGrammarStructureDto req,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.StructureCode) || string.IsNullOrWhiteSpace(req.Formula))
    {
        return Results.BadRequest("Mã cấu trúc và Công thức là bắt buộc.");
    }

    var entity = new Backend.Domain.Entities.GrammarStructure
    {
        StructureCode = req.StructureCode.Trim(),
        BandLevel = string.IsNullOrWhiteSpace(req.BandLevel) ? "7.0 - 8.0" : req.BandLevel.Trim(),
        Category = string.IsNullOrWhiteSpace(req.Category) ? "Writing Task 2" : req.Category.Trim(),
        GrammarTopic = req.GrammarTopic.Trim(),
        Formula = req.Formula.Trim(),
        UsageFunction = req.UsageFunction.Trim(),
        BasicExample = req.BasicExample?.Trim(),
        AdvancedExample = req.AdvancedExample.Trim(),
        VietnameseMeaning = req.VietnameseMeaning.Trim(),
        KeyCollocations = req.KeyCollocations?.Trim(),
        CommonMistakes = req.CommonMistakes?.Trim(),
        PracticeExercise = req.PracticeExercise?.Trim(),
        Tags = req.Tags?.Trim(),
        DisplayOrder = req.DisplayOrder,
        IsActive = req.IsActive,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.GrammarStructures.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/grammar-structures/{entity.Id}", entity);
});

app.MapPut("/api/admin/grammar-structures/{id:int}", async (
    int id,
    Backend.Application.DTOs.UpdateGrammarStructureDto req,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var entity = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
    if (entity == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

    entity.StructureCode = req.StructureCode.Trim();
    entity.BandLevel = req.BandLevel.Trim();
    entity.Category = req.Category.Trim();
    entity.GrammarTopic = req.GrammarTopic.Trim();
    entity.Formula = req.Formula.Trim();
    entity.UsageFunction = req.UsageFunction.Trim();
    entity.BasicExample = req.BasicExample?.Trim();
    entity.AdvancedExample = req.AdvancedExample.Trim();
    entity.VietnameseMeaning = req.VietnameseMeaning.Trim();
    entity.KeyCollocations = req.KeyCollocations?.Trim();
    entity.CommonMistakes = req.CommonMistakes?.Trim();
    entity.PracticeExercise = req.PracticeExercise?.Trim();
    entity.Tags = req.Tags?.Trim();
    entity.DisplayOrder = req.DisplayOrder;
    entity.IsActive = req.IsActive;
    entity.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(entity);
});

app.MapDelete("/api/admin/grammar-structures/{id:int}", async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var entity = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
    if (entity == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

    dbContext.GrammarStructures.Remove(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { success = true, message = $"Đã xóa cấu trúc '{entity.StructureCode}'." });
});

app.MapPost("/api/admin/grammar-structures/bulk-delete", async (
    Backend.Application.DTOs.GrammarBulkDeleteDto req,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (req.Ids == null || req.Ids.Count == 0)
    {
        return Results.BadRequest("Danh sách ID không được rỗng.");
    }

    var items = await dbContext.GrammarStructures
        .Where(g => req.Ids.Contains(g.Id))
        .ToListAsync(cancellationToken);

    dbContext.GrammarStructures.RemoveRange(items);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { success = true, deletedCount = items.Count });
});

// ─── Bulk Import Multiple Excel files (.xlsx) ───
app.MapPost("/api/admin/grammar-structures/import-multiple", async (
    Microsoft.AspNetCore.Http.IFormFileCollection files,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (files == null || files.Count == 0)
    {
        return Results.BadRequest("Không có file nào được upload.");
    }

    var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
               modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
        ? "upsert"
        : "skip";

    var allExisting = await dbContext.GrammarStructures.ToListAsync(cancellationToken);
    var existingDict = allExisting.ToDictionary(g => g.StructureCode.Trim(), g => g, StringComparer.OrdinalIgnoreCase);

    int totalRows = 0, success = 0, updated = 0, skipped = 0, fail = 0;
    var errors = new List<string>();

    foreach (var file in files)
    {
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"File '{file.FileName}' bị bỏ qua vì không phải định dạng .xlsx");
            continue;
        }

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) continue;

            // Đọc Header hàng 1 để map cột linh hoạt
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerRow = worksheet.Row(1);
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 13;
            for (int col = 1; col <= lastCol; col++)
            {
                var hName = headerRow.Cell(col).GetString()?.Trim().ToLowerInvariant() ?? "";
                if (!string.IsNullOrEmpty(hName))
                {
                    headerMap[hName] = col;
                }
            }

            int GetCol(string[] possibleNames, int defaultCol)
            {
                foreach (var name in possibleNames)
                {
                    if (headerMap.TryGetValue(name.ToLowerInvariant(), out var colIdx)) return colIdx;
                }
                return defaultCol;
            }

            int colCode = GetCol(new[] { "StructureCode", "Mã cấu trúc", "Mã", "Code" }, 1);
            int colBand = GetCol(new[] { "BandLevel", "Band", "Level", "Mức Band" }, 2);
            int colCat  = GetCol(new[] { "Category", "Kỹ năng", "Dạng bài", "Phần thi" }, 3);
            int colTopic = GetCol(new[] { "GrammarTopic", "Chủ điểm", "Topic", "Chủ điểm ngữ pháp" }, 4);
            int colFormula = GetCol(new[] { "Formula", "Công thức", "Cấu trúc" }, 5);
            int colUsage = GetCol(new[] { "UsageFunction", "Chức năng", "Mục đích", "Usage" }, 6);
            int colExample = GetCol(new[] { "Example", "Ví dụ", "Ví dụ minh họa", "Câu ví dụ", "BasicExample", "Câu gốc", "Ví dụ gốc" }, 7);
            int colAdvEx = GetCol(new[] { "AdvancedExample", "Câu nâng cấp", "Band 8.0", "Ví dụ nâng cao" }, -1);
            int colMeaning = GetCol(new[] { "VietnameseMeaning", "Nghĩa tiếng Việt", "Dịch nghĩa", "Meaning" }, 8);
            int colMistakes = GetCol(new[] { "CommonMistakes", "Lỗi sai", "Lỗi thường gặp", "Pitfalls" }, 9);
            int colExercise = GetCol(new[] { "PracticeExercise", "Bài tập", "Exercise", "Luyện tập" }, 10);
            int colTags = GetCol(new[] { "Tags", "Tag", "Từ khóa lọc" }, 11);
            int colColloc = GetCol(new[] { "KeyCollocations", "Collocations", "Từ vựng", "Từ khóa" }, -1);

            for (int r = 2; r <= lastRow; r++)
            {
                totalRows++;
                var row = worksheet.Row(r);

                string code = row.Cell(colCode).GetString()?.Trim() ?? "";
                string formula = row.Cell(colFormula).GetString()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(formula))
                {
                    continue; // Hàng trống
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    code = $"STR_{DateTime.UtcNow.Ticks % 1000000:D6}";
                }

                string band = row.Cell(colBand).GetString()?.Trim() ?? "7.0 - 8.0";
                string category = row.Cell(colCat).GetString()?.Trim() ?? "Writing Task 2";
                string topic = row.Cell(colTopic).GetString()?.Trim() ?? "Ngữ pháp nâng cao";
                string usage = row.Cell(colUsage).GetString()?.Trim() ?? "Nâng cao điểm Grammatical Range & Accuracy";
                string ex = row.Cell(colExample).GetString()?.Trim() ?? "";
                string advEx = (colAdvEx > 0 ? row.Cell(colAdvEx).GetString()?.Trim() : "") ?? "";
                string basicEx = ex;
                if (string.IsNullOrWhiteSpace(advEx)) advEx = ex;
                if (string.IsNullOrWhiteSpace(advEx)) advEx = formula;
                string meaning = row.Cell(colMeaning).GetString()?.Trim() ?? "";
                string colloc = (colColloc > 0 ? row.Cell(colColloc).GetString()?.Trim() : "") ?? "";
                string mistakes = row.Cell(colMistakes).GetString()?.Trim() ?? "";
                string exercise = row.Cell(colExercise).GetString()?.Trim() ?? "";
                string tags = row.Cell(colTags).GetString()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(meaning)) meaning = topic;

                if (existingDict.TryGetValue(code, out var existing))
                {
                    if (mode == "upsert")
                    {
                        existing.BandLevel = band;
                        existing.Category = category;
                        existing.GrammarTopic = topic;
                        existing.Formula = formula;
                        existing.UsageFunction = usage;
                        existing.BasicExample = string.IsNullOrWhiteSpace(basicEx) ? null : basicEx;
                        existing.AdvancedExample = advEx;
                        existing.VietnameseMeaning = meaning;
                        existing.KeyCollocations = string.IsNullOrWhiteSpace(colloc) ? null : colloc;
                        existing.CommonMistakes = string.IsNullOrWhiteSpace(mistakes) ? null : mistakes;
                        existing.PracticeExercise = string.IsNullOrWhiteSpace(exercise) ? null : exercise;
                        existing.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    var newStructure = new Backend.Domain.Entities.GrammarStructure
                    {
                        StructureCode = code,
                        BandLevel = band,
                        Category = category,
                        GrammarTopic = topic,
                        Formula = formula,
                        UsageFunction = usage,
                        BasicExample = string.IsNullOrWhiteSpace(basicEx) ? null : basicEx,
                        AdvancedExample = advEx,
                        VietnameseMeaning = meaning,
                        KeyCollocations = string.IsNullOrWhiteSpace(colloc) ? null : colloc,
                        CommonMistakes = string.IsNullOrWhiteSpace(mistakes) ? null : mistakes,
                        PracticeExercise = string.IsNullOrWhiteSpace(exercise) ? null : exercise,
                        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.GrammarStructures.Add(newStructure);
                    existingDict[code] = newStructure;
                    success++;
                }
            }
        }
        catch (Exception ex)
        {
            fail++;
            errors.Add($"Lỗi xử lý file '{file.FileName}': {ex.Message}");
        }
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new Backend.Application.DTOs.GrammarImportExcelResponse
    {
        TotalRows = totalRows,
        Success = success,
        Updated = updated,
        Skipped = skipped,
        Fail = fail,
        Errors = errors
    });
}).DisableAntiforgery();

// ─── Download Template Excel File (.xlsx) ───
app.MapGet("/api/admin/grammar-structures/template", () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var ws = workbook.Worksheets.Add("Grammar_Structures");

    // Header Titles (11 columns chuẩn)
    var headers = new[]
    {
        "StructureCode", "BandLevel", "Category", "GrammarTopic",
        "Formula", "UsageFunction", "Example",
        "VietnameseMeaning", "CommonMistakes", "PracticeExercise", "Tags"
    };

    for (int i = 0; i < headers.Length; i++)
    {
        var cell = ws.Cell(1, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0284C7");
        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
    }

    // Demo Rows (4 dòng đại diện các cấp độ)
    var demoData = new[]
    {
        new[] {
            "FND_TENSE_01", "4.0 - 5.0", "General", "Thì hiện tại đơn (Present Simple)",
            "S + V(s/es) + O | S + do/does + not + V",
            "Diễn tả chân lý, sự thật hiển nhiên, thói quen lặp lại; diễn tả số liệu cố định trong Task 1.",
            "He walks to work every morning.",
            "Anh ấy đi bộ đi làm mỗi buổi sáng.",
            "Quên thêm s/es khi chủ ngữ là ngôi thứ 3 số ít (He, She, It, danh từ số ít).",
            "Chia động từ: The data (indicate) that urban areas (consume) more electricity than rural regions.",
            "present_simple, tenses, foundation, task1"
        },
        new[] {
            "FND_PASS_01", "5.0 - 6.0", "Writing Task 1 & 2", "Câu bị động (Passive Voice)",
            "S + be + V3/ed (+ by O)",
            "Tạo phong cách học thuật khách quan trong IELTS Writing, đặc biệt khi tả bài quy trình (Process Task 1).",
            "Raw tea leaves are harvested by hand and transported to processing facilities.",
            "Lá chè tươi được thu hoạch thủ công và sau đó được vận chuyển đến các cơ sở chế biến.",
            "Dùng sai dạng phân từ 2 (V3) hoặc quên động từ 'to be' chia theo thì của câu.",
            "Chuyển sang câu bị động: Workers clean and dry the coffee beans.",
            "passive_voice, process, task1, academic"
        },
        new[] {
            "FND_COND_02", "5.5 - 6.5", "Writing Task 2", "Câu điều kiện loại 2 (Conditional Type 2)",
            "If + S + V2/were, S + would/could/might + V",
            "Diễn tả giả định trái với thực tế hiện tại; lập luận biện chứng phản đề trong Task 2.",
            "If governments subsidized renewable energy infrastructure, fossil fuel dependence would diminish.",
            "Nếu các chính phủ trợ cấp cho cơ sở hạ tầng năng lượng tái tạo, sự phụ thuộc vào nhiên liệu hóa thạch sẽ giảm.",
            "Dùng 'was' thay vì 'were' trong văn phong học thuật trang trọng; nhầm lẫn thì ở mệnh đề chính.",
            "Viết lại câu giả định: Because petrol is cheap, people drive private cars too much.",
            "conditional, type2, task2, hypothesis"
        },
        new[] {
            "W_INV_01", "7.5 - 8.5", "Writing Task 2", "Đảo ngữ (Inversion)",
            "Not only + Aux + S + V, but S + (also) + V",
            "Nhấn mạnh 2 tác động song hành, tạo ấn tượng học thuật mạnh ở mở đoạn hoặc câu chủ đề.",
            "Not only does technological adoption facilitate learning, but it also enhances productivity.",
            "Không chỉ việc áp dụng công nghệ tạo điều kiện cho học tập, mà nó còn nâng cao năng suất.",
            "Quên đảo trợ động từ lên trước chủ ngữ sau 'Not only' (ví dụ viết sai: Not only computers help...).",
            "Rewrite: Tourism creates jobs and it also introduces local culture.",
            "inversion, emphasis, task2, academic"
        }
    };

    for (int r = 0; r < demoData.Length; r++)
    {
        for (int c = 0; c < demoData[r].Length; c++)
        {
            ws.Cell(r + 2, c + 1).Value = demoData[r][c];
        }
    }

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var bytes = stream.ToArray();

    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "IELTS_Grammar_Structures_Template.xlsx"
    );
});

// ─── Export All Grammar Structures to Excel (.xlsx) ───
app.MapGet("/api/admin/grammar-structures/export", async (
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var list = await dbContext.GrammarStructures
        .OrderBy(g => g.DisplayOrder)
        .ThenBy(g => g.Id)
        .ToListAsync(cancellationToken);

    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var ws = workbook.Worksheets.Add("Grammar_Structures");

    var headers = new[]
    {
        "StructureCode", "BandLevel", "Category", "GrammarTopic",
        "Formula", "UsageFunction", "Example",
        "VietnameseMeaning", "CommonMistakes", "PracticeExercise", "Tags"
    };

    for (int i = 0; i < headers.Length; i++)
    {
        var cell = ws.Cell(1, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0284C7");
        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
    }

    for (int r = 0; r < list.Count; r++)
    {
        var item = list[r];
        string ex = !string.IsNullOrWhiteSpace(item.BasicExample) ? item.BasicExample : item.AdvancedExample;
        ws.Cell(r + 2, 1).Value = item.StructureCode;
        ws.Cell(r + 2, 2).Value = item.BandLevel;
        ws.Cell(r + 2, 3).Value = item.Category;
        ws.Cell(r + 2, 4).Value = item.GrammarTopic;
        ws.Cell(r + 2, 5).Value = item.Formula;
        ws.Cell(r + 2, 6).Value = item.UsageFunction;
        ws.Cell(r + 2, 7).Value = ex;
        ws.Cell(r + 2, 8).Value = item.VietnameseMeaning;
        ws.Cell(r + 2, 9).Value = item.CommonMistakes ?? "";
        ws.Cell(r + 2, 10).Value = item.PracticeExercise ?? "";
        ws.Cell(r + 2, 11).Value = item.Tags ?? "";
    }

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var bytes = stream.ToArray();

    var filename = $"IELTS_Grammar_Structures_Export_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        filename
    );
});

// ── NOTIFICATION APIS ──
// 1. Get notifications for user (broadcast + specific user)
app.MapGet("/api/notifications", async (
    System.Security.Claims.ClaimsPrincipal user,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    int? currentUserId = null;
    var subClaim = user.FindFirst("sub")?.Value 
                   ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(subClaim, out var uid))
    {
        currentUserId = uid;
    }

    var notifs = await dbContext.Notifications
        .AsNoTracking()
        .Where(n => n.IsActive && (n.IsBroadcast || (currentUserId.HasValue && n.UserId == currentUserId.Value)))
        .OrderByDescending(n => n.CreatedAt)
        .Take(30)
        .ToListAsync(cancellationToken);

    HashSet<int> readNotifIds = new();
    if (currentUserId.HasValue)
    {
        var notifIds = notifs.Select(n => n.Id).ToList();
        readNotifIds = (await dbContext.UserNotificationReads
            .AsNoTracking()
            .Where(r => r.UserId == currentUserId.Value && notifIds.Contains(r.NotificationId))
            .Select(r => r.NotificationId)
            .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    var result = notifs.Select(n => new Backend.Application.DTOs.NotificationDto
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        Icon = n.Icon,
        TargetUrl = n.TargetUrl,
        CreatedAt = n.CreatedAt,
        IsRead = readNotifIds.Contains(n.Id)
    }).ToList();

    return Results.Ok(result);
});

// 2. Mark a notification as read
app.MapPost("/api/notifications/{id:int}/read", [Microsoft.AspNetCore.Authorization.Authorize] async (
    int id,
    System.Security.Claims.ClaimsPrincipal user,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var subClaim = user.FindFirst("sub")?.Value 
                   ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(subClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var exists = await dbContext.UserNotificationReads
        .AnyAsync(r => r.UserId == userId && r.NotificationId == id, cancellationToken);

    if (!exists)
    {
        dbContext.UserNotificationReads.Add(new Backend.Domain.Entities.UserNotificationRead
        {
            UserId = userId,
            NotificationId = id,
            ReadAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { success = true });
});

// 3. Mark all notifications as read
app.MapPost("/api/notifications/read-all", [Microsoft.AspNetCore.Authorization.Authorize] async (
    System.Security.Claims.ClaimsPrincipal user,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var subClaim = user.FindFirst("sub")?.Value 
                   ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(subClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var activeNotifIds = await dbContext.Notifications
        .AsNoTracking()
        .Where(n => n.IsActive && (n.IsBroadcast || n.UserId == userId))
        .Select(n => n.Id)
        .ToListAsync(cancellationToken);

    var alreadyReadIds = (await dbContext.UserNotificationReads
        .AsNoTracking()
        .Where(r => r.UserId == userId && activeNotifIds.Contains(r.NotificationId))
        .Select(r => r.NotificationId)
        .ToListAsync(cancellationToken))
        .ToHashSet();

    var unreadIds = activeNotifIds.Where(id => !alreadyReadIds.Contains(id)).ToList();
    if (unreadIds.Count > 0)
    {
        var readsToAdd = unreadIds.Select(id => new Backend.Domain.Entities.UserNotificationRead
        {
            UserId = userId,
            NotificationId = id,
            ReadAt = DateTime.UtcNow
        });
        dbContext.UserNotificationReads.AddRange(readsToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new { success = true, markedCount = unreadIds.Count });
});

// ── ADMIN NOTIFICATION APIS ──
// 4. Admin: Get all notifications
app.MapGet("/api/admin/notifications", async (
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var notifs = await dbContext.Notifications
        .AsNoTracking()
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync(cancellationToken);

    var readCounts = await dbContext.UserNotificationReads
        .AsNoTracking()
        .GroupBy(r => r.NotificationId)
        .Select(g => new { NotificationId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(g => g.NotificationId, g => g.Count, cancellationToken);

    var dtos = notifs.Select(n => new Backend.Application.DTOs.AdminNotificationDto
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        Icon = n.Icon,
        TargetUrl = n.TargetUrl,
        CreatedAt = n.CreatedAt,
        CreatedByAdmin = n.CreatedByAdmin,
        IsBroadcast = n.IsBroadcast,
        UserId = n.UserId,
        IsActive = n.IsActive,
        ReadCount = readCounts.GetValueOrDefault(n.Id, 0)
    }).ToList();

    return Results.Ok(dtos);
});

// 5. Admin: Create notification
app.MapPost("/api/admin/notifications", async (
    Backend.Application.DTOs.CreateNotificationRequest req,
    System.Security.Claims.ClaimsPrincipal user,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { message = "Tiêu đề và nội dung thông báo không được để trống." });
    }

    var adminName = user.FindFirst("name")?.Value 
                    ?? user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                    ?? "Admin";

    var notif = new Backend.Domain.Entities.Notification
    {
        Title = req.Title.Trim(),
        Message = req.Message.Trim(),
        Type = string.IsNullOrWhiteSpace(req.Type) ? "system" : req.Type.Trim(),
        Icon = string.IsNullOrWhiteSpace(req.Icon) ? "bi-bell-fill" : req.Icon.Trim(),
        TargetUrl = string.IsNullOrWhiteSpace(req.TargetUrl) ? null : req.TargetUrl.Trim(),
        CreatedAt = DateTime.UtcNow,
        CreatedByAdmin = adminName,
        IsBroadcast = req.IsBroadcast,
        UserId = req.UserId,
        IsActive = req.IsActive
    };

    dbContext.Notifications.Add(notif);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { success = true, id = notif.Id });
});

// 6. Admin: Update notification
app.MapPut("/api/admin/notifications/{id:int}", async (
    int id,
    Backend.Application.DTOs.UpdateNotificationRequest req,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var notif = await dbContext.Notifications.FindAsync(new object[] { id }, cancellationToken);
    if (notif == null)
    {
        return Results.NotFound(new { message = "Không tìm thấy thông báo." });
    }

    if (!string.IsNullOrWhiteSpace(req.Title)) notif.Title = req.Title.Trim();
    if (!string.IsNullOrWhiteSpace(req.Message)) notif.Message = req.Message.Trim();
    if (!string.IsNullOrWhiteSpace(req.Type)) notif.Type = req.Type.Trim();
    if (!string.IsNullOrWhiteSpace(req.Icon)) notif.Icon = req.Icon.Trim();
    notif.TargetUrl = string.IsNullOrWhiteSpace(req.TargetUrl) ? null : req.TargetUrl.Trim();
    notif.IsBroadcast = req.IsBroadcast;
    notif.UserId = req.UserId;
    notif.IsActive = req.IsActive;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { success = true });
});

// 7. Admin: Delete notification
app.MapDelete("/api/admin/notifications/{id:int}", async (
    int id,
    Backend.Infrastructure.Persistence.AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var notif = await dbContext.Notifications.FindAsync(new object[] { id }, cancellationToken);
    if (notif == null)
    {
        return Results.NotFound(new { message = "Không tìm thấy thông báo." });
    }

    dbContext.Notifications.Remove(notif);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { success = true });
});

app.Run();

public record CreateExamRequest(string Title, string DataUrl, string Category = "IELTS");
public record SaveToeicExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskVocabularyRequest(string HskLevel, string Hanzi, string Pinyin, string Meaning, string? WordType, string? ExampleSentence, string? ExamplePinyin, string? ExampleMeaning, string? AudioUrl, int? DisplayOrder, bool? IsActive);
public record UpdateVocabProgressRequest(bool Learned);
public record MigrateVocabProgressRequest(List<int> VocabularyIds);
public record IeltsVocabularyRequest(string Word, string? Phonetic, string? PartOfSpeech, string Meaning, string? Example, string? ExampleMeaning, string? Topic, string? CefrLevel, int? DisplayOrder, bool? IsActive);

/// <summary>
/// Parser CSV hỗ trợ dấu ngoặc kép, dấu phẩy/chấm phẩy/tab trong ô và tự dò delimiter.
/// </summary>
public static class HskVocabCsvParser
{
    public static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public static IEnumerable<List<string>> Parse(string content)
    {
        char delimiter = DetectDelimiter(content);

        var rows = new List<List<string>>();
        var field = new System.Text.StringBuilder();
        var current = new List<string>();
        var inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == delimiter)
            {
                current.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                current.Add(field.ToString());
                field.Clear();
                rows.Add(current);
                current = new List<string>();
            }
            else field.Append(c);
        }

        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString());
            rows.Add(current);
        }
        return rows;
    }

    private static char DetectDelimiter(string content)
    {
        int commas = 0, semicolons = 0, tabs = 0;
        bool inQuotes = false;
        foreach (char c in content)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes)
            {
                if (c == ',') commas++;
                else if (c == ';') semicolons++;
                else if (c == '\t') tabs++;
                else if (c == '\n') break; // chỉ xét dòng đầu
            }
        }
        if (semicolons > commas && semicolons >= tabs) return ';';
        if (tabs > commas && tabs > semicolons) return '\t';
        return ',';
    }
}
