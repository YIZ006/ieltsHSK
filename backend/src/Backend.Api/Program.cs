using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCorsPolicy";
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "https://localhost:7102";
const string frontendHttpUrl = "http://localhost:5102";

builder.Services.AddInfrastructure(builder.Configuration);
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

app.MapPost("/api/listen-videos/submit", async (Backend.Application.DTOs.ListenVideoSubmitRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService ytService, CancellationToken cancellationToken) =>
{
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
    catch (Exception ex)
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

app.MapGet("/api/admin/listen-videos", async (Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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

app.MapPut("/api/admin/listen-videos/{id}/approve", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var video = await dbContext.ListenVideos.FindAsync(new object[] { id }, cancellationToken);
    if (video == null) return Results.NotFound();

    video.IsApproved = true;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { Message = "Đã duyệt video thành công" });
});

app.MapPut("/api/admin/listen-videos/{id}/transcript", async (int id, Backend.Application.DTOs.ManualTranscriptRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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

app.MapDelete("/api/admin/listen-videos/{id}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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

app.MapGet("/api/admin/listen-videos/template-excel", () =>
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("ListenVideos");
    
    // Header
    worksheet.Cell(1, 1).Value = "Youtube Link";
    worksheet.Cell(1, 2).Value = "Transcript (Tiếng Anh)";
    
    // Header styling
    var headerRange = worksheet.Range("A1:B1");
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
    
    // Sample data
    worksheet.Cell(2, 1).Value = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    worksheet.Cell(2, 2).Value = "Never gonna give you up, never gonna let you down...";
    
    worksheet.Column(1).Width = 50;
    worksheet.Column(2).Width = 100;
    
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ListenVideos_Template.xlsx");
});

app.MapPost("/api/admin/listen-videos/import-excel", async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Infrastructure.Services.YoutubeTranscriptService transcriptService, Backend.Application.Abstractions.IR2StorageService r2Storage, CancellationToken cancellationToken) =>
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
            
            var url = urlCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(url) && urlCell.HasHyperlink)
                url = urlCell.GetHyperlink().ExternalAddress?.ToString()?.Trim();
                
            var transcript = transcriptCell.GetString()?.Trim();
            
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
                
                var fileName = $"listen-videos/transcript_{Guid.NewGuid().ToString().Substring(0, 4)}_{safeTitle}.json";
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
                Level = "Intermediate",
                Category = "General",
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



app.MapPost("/api/ielts/exams", async (CreateExamRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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

app.MapPost("/api/mock-tests", async (Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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
        ToeicUrl = request.ToeicUrl
    };
    
    dbContext.MockTests.Add(newTest);
    await dbContext.SaveChangesAsync(cancellationToken);
    
    return Results.Ok(new { Id = newTest.Id });
});

app.MapPut("/api/mock-tests/{id}", async (int id, Backend.Application.DTOs.CreateMockTestRequest request, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

app.MapDelete("/api/mock-tests/{id}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
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

app.MapPost("/api/mock-tests/upload", async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
        
    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
    using var stream = file.OpenReadStream();
    
    try
    {
        var url = await r2Service.UploadFileAsync(stream, fileName, file.ContentType, cancellationToken);
        return Results.Ok(new { Url = url });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).DisableAntiforgery(); // Disable Anti-forgery for API upload if needed

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
app.MapPost("/api/toeic/upload-media", async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
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
app.MapPost("/api/toeic/save-exam", async (
        SaveToeicExamRequest req,
        Backend.Application.Abstractions.IR2StorageService r2Service,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.CollectionName) || string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest("CollectionName and Title are required.");

    var json = System.Text.Json.JsonSerializer.Serialize(req.ExamData,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
    var fileId = Guid.NewGuid().ToString("N");
    var fileName = $"toeic/exams/{fileId}.json";

    string jsonUrl;
    try
    {
        using var ms = new MemoryStream(jsonBytes);
        jsonUrl = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
    }
    catch
    {
        // Fallback: lưu local nếu R2 lỗi (dev only)
        var dir = Path.Combine("wwwroot", "exports");
        Directory.CreateDirectory(dir);
        var localPath = Path.Combine(dir, $"{fileId}.json");
        await File.WriteAllBytesAsync(localPath, jsonBytes, cancellationToken);
        jsonUrl = $"/exports/{fileId}.json";
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
app.MapPost("/api/hsk/upload-media", async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Application.Abstractions.IR2StorageService r2Service, CancellationToken cancellationToken) =>
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
app.MapPost("/api/hsk/save-exam", async (
        HskSaveExamRequest req,
        Backend.Application.Abstractions.IR2StorageService r2Service,
        Backend.Infrastructure.Persistence.AppDbContext dbContext,
        CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(req.CollectionName) || string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest("CollectionName and Title are required.");

    var json = System.Text.Json.JsonSerializer.Serialize(req.ExamData,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
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

app.MapPost("/api/hsk/vocab", async (HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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

app.MapPut("/api/hsk/vocab/{id}", async (int id, HskVocabularyRequest req, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
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

app.MapDelete("/api/hsk/vocab/{id}", async (int id, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var vocab = await dbContext.HskVocabularies.FindAsync(new object[] { id }, cancellationToken);
    if (vocab == null) return Results.NotFound();
    dbContext.HskVocabularies.Remove(vocab);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok();
});

// ─── HSK Vocabulary Excel Import ───
app.MapGet("/api/hsk/vocab/template-excel", () =>
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

app.MapPost("/api/hsk/vocab/import-excel", async (Microsoft.AspNetCore.Http.IFormFile file, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("File không hợp lệ hoặc trống.");
    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Vui lòng upload file Excel (.xlsx)");

    int success = 0, fail = 0, duplicate = 0;
    var errors = new List<string>();

    using var stream = file.OpenReadStream();
    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
    var worksheet = workbook.Worksheet(1);
    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

    for (int i = 2; i <= lastRow; i++)
    {
        try
        {
            var row = worksheet.Row(i);
            var level = row.Cell(1).GetString()?.Trim();
            var hanzi = row.Cell(2).GetString()?.Trim();
            if (string.IsNullOrEmpty(level) || string.IsNullOrEmpty(hanzi)) continue;

            // Check duplicate
            bool exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                dbContext.HskVocabularies, v => v.HskLevel == level && v.Hanzi == hanzi, cancellationToken);
            if (exists) { duplicate++; continue; }

            var vocab = new Backend.Domain.Entities.HskVocabulary
            {
                HskLevel = level,
                Hanzi = hanzi,
                Pinyin = row.Cell(3).GetString()?.Trim() ?? "",
                Meaning = row.Cell(4).GetString()?.Trim() ?? "",
                WordType = row.Cell(5).GetString()?.Trim(),
                ExampleSentence = row.Cell(6).GetString()?.Trim(),
                ExamplePinyin = row.Cell(7).GetString()?.Trim(),
                ExampleMeaning = row.Cell(8).GetString()?.Trim(),
                AudioUrl = row.Cell(9).GetString()?.Trim(),
                DisplayOrder = int.TryParse(row.Cell(10).GetString(), out int order) ? order : 0,
                IsActive = true
            };
            dbContext.HskVocabularies.Add(vocab);
            success++;
        }
        catch (Exception ex)
        {
            errors.Add($"Dòng {i}: {ex.Message}");
            fail++;
        }
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    var msg = $"Nhập thành công {success}, thất bại {fail}, bỏ qua {duplicate} bị trùng.";
    if (errors.Any()) msg += " Chi tiết: " + string.Join(" | ", errors.Take(3));
    return Results.Ok(new { Success = success, Fail = fail, Duplicate = duplicate, Errors = errors });
}).DisableAntiforgery();

app.Run();

public record CreateExamRequest(string Title, string DataUrl, string Category = "IELTS");
public record SaveToeicExamRequest(string CollectionName, string Title, int? MockTestId, System.Text.Json.JsonElement ExamData);
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, System.Text.Json.JsonElement ExamData);
public record HskVocabularyRequest(string HskLevel, string Hanzi, string Pinyin, string Meaning, string? WordType, string? ExampleSentence, string? ExamplePinyin, string? ExampleMeaning, string? AudioUrl, int? DisplayOrder, bool? IsActive);
