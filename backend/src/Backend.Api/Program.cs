using Backend.Api;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "https://localhost:7102";
const string frontendHttpUrl = "http://localhost:5102";

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(frontendUrl, frontendHttpUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
        // Chặn token của tài khoản bị khoá/xoá ngay cả khi JWT còn hạn
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();
                // Tuỳ cấu hình MapInboundClaims, claim "sub" có thể bị đổi tên thành NameIdentifier
                var userIdClaim = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var user = await dbContext.Users.FindAsync(new object[] { userId }, context.HttpContext.RequestAborted);
                if (user == null || !user.IsActive)
                {
                    context.Fail("Account is disabled or no longer exists.");
                }
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
await app.Services.SeedDataAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/ielts/courses", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
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
    return Results.Ok(courses);
});

app.MapGet("/api/ielts/websites", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
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
    return Results.Ok(websites);
});

app.MapGet("/api/ielts/sections", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
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
    return Results.Ok(sections);
});

// LISTEN VIDEOS API
app.MapGet("/api/listen-videos", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
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
        [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.ListenVideoSubmitRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService ytService, CancellationToken cancellationToken) =>
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
        SubmittedByUserId = "User"
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    video.IsApproved = true;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Message = "Đã duyệt video thành công" });
});

app.MapPut("/api/admin/listen-videos/{id}/transcript",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Application.DTOs.ManualTranscriptRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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
        return Results.Ok(new { Message = "Cập nhật phụ đề thành công", TranscriptUrl = r2Url, WordCount = wordCount });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Lỗi khi xử lý phụ đề thủ công: " + ex.Message });
    }
});

app.MapDelete("/api/admin/listen-videos/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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

    return Results.Ok(new { Message = "Video removed successfully" });
});

app.MapPut("/api/admin/listen-videos/{id}", async (int id, Backend.Application.DTOs.UpdateListenVideoRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(req.Title)) video.Title = req.Title.Trim();
    if (!string.IsNullOrWhiteSpace(req.Level)) video.Level = req.Level.Trim();
    if (!string.IsNullOrWhiteSpace(req.Category)) video.Category = req.Category.Trim();

    await dbContext.SaveChangesAsync(cancellationToken);
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (CreateExamRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var exam = new Backend.Domain.Entities.Exam
    {
        Title = request.Title,
        DataUrl = request.DataUrl,
        Category = request.Category
    };

    dbContext.Exams.Add(exam);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { exam.Id, exam.Title, exam.DataUrl });
});

app.MapGet("/api/ielts/exams", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var exams = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        dbContext.Exams
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.DataUrl,
                e.Category,
                e.CreatedAt
            }), cancellationToken);
    
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
});

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
});

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
});

app.MapPut("/api/user/level", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateLevelRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    if (int.TryParse(userIdString, out int userId))
    {
        var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (dbUser != null)
        {
            dbUser.Level = request.Level;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok();
        }
    }
    return Results.Unauthorized();
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
        string payload;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        // Chống stored XSS: làm sạch nội dung JSON đề thi trước khi lưu lên R2
        var isJson = (file.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase)
                     || file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        var clean = isJson ? HtmlGuard.SanitizeJsonStrings(payload) : payload;

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clean));
        var url = await r2Service.UploadFileAsync(stream, fileName, contentType, cancellationToken);
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

app.MapPost("/api/test-submissions", async (Backend.Application.DTOs.CreateTestSubmissionRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var submission = new Backend.Domain.Entities.TestSubmission
    {
        UserId = request.UserId,
        SessionId = request.SessionId,
        Skill = request.Skill,
        ExamUrl = request.ExamUrl,
        BandScore = request.BandScore,
        CorrectCount = request.CorrectCount,
        TotalCount = request.TotalCount,
        DetailsJson = request.DetailsJson,
        SubmittedAt = DateTimeOffset.UtcNow
    };

    dbContext.TestSubmissions.Add(submission);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { Id = submission.Id });
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
    var user = await dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user == null) return Results.NotFound();

    user.Username = request.Username;
    user.Email = request.Email;
    user.Role = request.Role;
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
app.MapGet("/api/ielts/vocab", async (string? topic, string? search, Backend.Infrastructure.Persistence.AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    const string cacheKey = "IeltsVocabularyList";
    var cacheOptions = new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromMinutes(5))
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

    var allItems = await cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.SetOptions(cacheOptions);
        var items = await dbContext.IeltsVocabularies
            .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);
        return items.Select(v => new
        {
            v.Id, v.Word, v.Phonetic, v.PartOfSpeech, v.Meaning,
            v.Example, v.ExampleMeaning, v.Topic, v.DisplayOrder, v.IsActive, v.CreatedAt
        }).ToList();
    });

    var query = allItems!.AsEnumerable();
    if (!string.IsNullOrEmpty(topic))
        query = query.Where(v => v.Topic == topic);
    if (!string.IsNullOrEmpty(search))
        query = query.Where(v => v.Word.Contains(search, StringComparison.OrdinalIgnoreCase) || v.Meaning.Contains(search, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(query);
});

app.MapPost("/api/ielts/vocab", async (IeltsVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
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
        DisplayOrder = req.DisplayOrder ?? 0,
        IsActive = req.IsActive ?? true
    };
    dbContext.IeltsVocabularies.Add(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);

    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    cache.Remove("IeltsVocabularyList");

    return Results.Ok(new { Id = vocab.Id });
});

app.MapPut("/api/ielts/vocab/{id:int}", async (int id, IeltsVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
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
    if (req.DisplayOrder.HasValue) vocab.DisplayOrder = req.DisplayOrder.Value;
    if (req.IsActive.HasValue) vocab.IsActive = req.IsActive.Value;
    await dbContext.SaveChangesAsync(cancellationToken);

    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    cache.Remove("IeltsVocabularyList");

    return Results.Ok();
});

app.MapDelete("/api/ielts/vocab/{id:int}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.IeltsVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();
    dbContext.IeltsVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);

    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    cache.Remove("IeltsVocabularyList");

    return Results.Ok();
});

// ─── IELTS: Vocabulary Excel Template ───
app.MapGet("/api/ielts/vocab/template-excel", () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("IELTS Vocabulary");
    string[] headers = { "Word", "Phonetic", "PartOfSpeech", "Meaning", "Example", "ExampleMeaning", "Topic", "DisplayOrder" };
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
    worksheet.Cell(2, 8).Value = 1;
    worksheet.Cell(3, 1).Value = "sustainable";
    worksheet.Cell(3, 2).Value = "/səˈsteɪnəbl/";
    worksheet.Cell(3, 3).Value = "adjective";
    worksheet.Cell(3, 4).Value = "bền vững";
    worksheet.Cell(3, 5).Value = "We need sustainable development to protect the environment.";
    worksheet.Cell(3, 6).Value = "Chúng ta cần phát triển bền vững để bảo vệ môi trường.";
    worksheet.Cell(3, 7).Value = "Environment";
    worksheet.Cell(3, 8).Value = 2;

    worksheet.Column(1).Width = 16;
    worksheet.Column(2).Width = 16;
    worksheet.Column(3).Width = 12;
    worksheet.Column(4).Width = 28;
    worksheet.Column(5).Width = 48;
    worksheet.Column(6).Width = 42;
    worksheet.Column(7).Width = 16;
    worksheet.Column(8).Width = 13;

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
                r.Cell(8).GetString()?.Trim() ?? ""
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

            var jsonItem = new
            {
                word,
                phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                partOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                meaning,
                example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                exampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                displayOrder = int.TryParse(cells[7], out int ordVal) ? ordVal : 0
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
                    int newOrd = int.TryParse(cells[7], out int ord) ? ord : existing.DisplayOrder;

                    if (existing.Phonetic == newPhonetic &&
                        existing.PartOfSpeech == newPos &&
                        existing.Example == newExample &&
                        existing.ExampleMeaning == newExampleMeaning &&
                        existing.Topic == newTopic &&
                        existing.DisplayOrder == newOrd)
                    {
                        duplicate++;
                        continue;
                    }

                    existing.Phonetic = newPhonetic;
                    existing.PartOfSpeech = newPos;
                    existing.Example = newExample;
                    existing.ExampleMeaning = newExampleMeaning;
                    existing.Topic = newTopic;
                    existing.DisplayOrder = newOrd;
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
                DisplayOrder = int.TryParse(cells[7], out int order) ? order : 0,
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

    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    cache.Remove("IeltsVocabularyList");

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
                r.Cell(8).GetString()?.Trim() ?? ""
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

            var jsonItem = new
            {
                word,
                phonetic = HskVocabCsvParser.NullIfEmpty(cells[1]),
                partOfSpeech = HskVocabCsvParser.NullIfEmpty(cells[2]),
                meaning,
                example = HskVocabCsvParser.NullIfEmpty(cells[4]),
                exampleMeaning = HskVocabCsvParser.NullIfEmpty(cells[5]),
                topic = HskVocabCsvParser.NullIfEmpty(cells[6]),
                displayOrder = int.TryParse(cells[7], out int ordVal) ? ordVal : 0
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
                    int newOrd = int.TryParse(cells[7], out int ord) ? ord : existing.DisplayOrder;

                    if (existing.Phonetic == newPhonetic &&
                        existing.PartOfSpeech == newPos &&
                        existing.Example == newExample &&
                        existing.ExampleMeaning == newExampleMeaning &&
                        existing.Topic == newTopic &&
                        existing.DisplayOrder == newOrd)
                    {
                        duplicate++;
                        continue;
                    }

                    existing.Phonetic = newPhonetic;
                    existing.PartOfSpeech = newPos;
                    existing.Example = newExample;
                    existing.ExampleMeaning = newExampleMeaning;
                    existing.Topic = newTopic;
                    existing.DisplayOrder = newOrd;
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
                DisplayOrder = int.TryParse(cells[7], out int order) ? order : 0,
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

    var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
    cache.Remove("IeltsVocabularyList");

    var msg = $"Thêm mới {success}, cập nhật {updated}, thất bại {fail}, bỏ qua {duplicate} trùng.";
    if (errors.Any()) msg += " Chi tiết: " + string.Join(" | ", errors.Take(3));
    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Updated = updated, Errors = errors, JsonUrl = jsonUrl });
}).DisableAntiforgery();

// ─── IELTS: Delete all vocabulary ───
app.MapDelete("/api/ielts/vocab/all", async (Backend.Infrastructure.Persistence.AppDbContext dbContext,
        ILogger<Program> logger,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
        HttpContext httpContext,
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
        var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
        cache.Remove("IeltsVocabularyList");

        return Results.Ok(new { Deleted = count, R2FilesDeleted = imports.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Delete all vocabulary failed");
        return Results.Problem($"Xóa thất bại: {ex.Message}");
    }
});

// ─── HSK: Vocabulary CRUD ───
app.MapGet("/api/hsk/vocab", async (string? level, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var query = dbContext.HskVocabularies.AsQueryable();
    if (!string.IsNullOrEmpty(level))
        query = query.Where(v => v.HskLevel == level);
    var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderBy(v => v.HskLevel).ThenBy(v => v.DisplayOrder), cancellationToken);
    return Results.Ok(items.Select(v => new
    {
        v.Id,
        v.HskLevel,
        v.Hanzi,
        v.Pinyin,
        v.Meaning,
        v.WordType,
        v.ExampleSentence,
        v.ExamplePinyin,
        v.ExampleMeaning,
        v.AudioUrl,
        v.DisplayOrder,
        v.IsActive,
        v.CreatedAt
    }));
});

app.MapPost("/api/hsk/vocab",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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
    return Results.Ok(new { Id = vocab.Id });
});

app.MapPut("/api/hsk/vocab/{id}",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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
    return Results.Ok();
});

app.MapPost("/api/admin/users/{id}/reset-password",
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    if (user == null) return Results.NotFound();

    var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8) + "@1Aa";
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.HskVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();
    dbContext.HskVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

// ─── HSK: Delete all vocabulary ───
app.MapDelete("/api/hsk/vocab/all", async (Backend.Infrastructure.Persistence.AppDbContext dbContext,
        ILogger<Program> logger,
        Backend.Application.Abstractions.IR2StorageService r2Storage,
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
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
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
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
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
    var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
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

    var msg = $"Thêm mới {success}, cập nhật {updated}, thất bại {fail}, bỏ qua {duplicate} trùng.";
    if (errors.Any()) msg += " Chi tiết: " + string.Join(" | ", errors.Take(3));
    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Updated = updated, Errors = errors, JsonUrl = jsonUrl });
}).DisableAntiforgery();

app.Run();

public record CreateExamRequest(string Title, string DataUrl, string Category = "IELTS");
public record SaveToeicExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskVocabularyRequest(string HskLevel, string Hanzi, string Pinyin, string Meaning, string? WordType, string? ExampleSentence, string? ExamplePinyin, string? ExampleMeaning, string? AudioUrl, int? DisplayOrder, bool? IsActive);
public record UpdateVocabProgressRequest(bool Learned);
public record MigrateVocabProgressRequest(List<int> VocabularyIds);
public record IeltsVocabularyRequest(string Word, string? Phonetic, string? PartOfSpeech, string Meaning, string? Example, string? ExampleMeaning, string? Topic, int? DisplayOrder, bool? IsActive);

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
