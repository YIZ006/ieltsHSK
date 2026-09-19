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

public static class ToeicEndpoints
{
    public static IEndpointRouteBuilder MapToeicEndpoints(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
