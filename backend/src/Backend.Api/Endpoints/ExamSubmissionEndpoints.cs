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

public static class ExamSubmissionEndpoints
{
    public static IEndpointRouteBuilder MapExamSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
