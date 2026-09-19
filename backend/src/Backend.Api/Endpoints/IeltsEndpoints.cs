using Backend.Api.Common;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Api.Endpoints;

public static class IeltsEndpoints
{
    public static IEndpointRouteBuilder MapIeltsEndpoints(this IEndpointRouteBuilder app)
    {
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


        return app;
    }
}
