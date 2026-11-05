using Backend.Api.Common;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Api.Endpoints;

public static class HskEndpoints
{
    public static IEndpointRouteBuilder MapHskEndpoints(this IEndpointRouteBuilder app)
    {
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
        }).RequireAuthorization(policy => policy.RequireRole("admin"));


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
        }).RequireAuthorization(policy => policy.RequireRole("admin"));


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
        }).RequireAuthorization(policy => policy.RequireRole("admin"));


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

        app.MapGet("/api/hsk/vocab", async (string? level, Backend.Infrastructure.Persistence.AppDbContext dbContext, ICacheService cacheService, CancellationToken cancellationToken) =>
        {
            var normalizedLevel = string.IsNullOrEmpty(level) ? null : level.Replace(" ", "").Trim();
            var cacheKey = normalizedLevel == null ? "hsk:vocab:all" : $"hsk:vocab:{normalizedLevel.ToLowerInvariant()}";
            var cached = await cacheService.GetAsync<List<HskVocabularyDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var query = dbContext.HskVocabularies.AsQueryable();
            if (!string.IsNullOrEmpty(level))
            {
                var norm = normalizedLevel!;
                query = query.Where(v => v.HskLevel == level || v.HskLevel == norm);
            }
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
        }).RequireAuthorization(policy => policy.RequireRole("admin"));

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

        // ─── HSK Vocabulary Review List (Sổ tay ôn tập từ vựng) ───

        app.MapGet("/api/hsk/review", [Microsoft.AspNetCore.Authorization.Authorize] async (
                System.Security.Claims.ClaimsPrincipal user,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var list = await dbContext.HskReviewWords
                .Where(r => r.UserId == userId)
                .Include(r => r.Vocabulary)
                .OrderByDescending(r => r.AddedAt)
                .Select(r => new Backend.Application.DTOs.HskReviewWordDto
                {
                    Id = r.Id,
                    VocabularyId = r.VocabularyId,
                    HskLevel = r.Vocabulary != null ? r.Vocabulary.HskLevel : string.Empty,
                    Hanzi = r.Vocabulary != null ? r.Vocabulary.Hanzi : string.Empty,
                    Pinyin = r.Vocabulary != null ? r.Vocabulary.Pinyin : string.Empty,
                    Meaning = r.Vocabulary != null ? r.Vocabulary.Meaning : string.Empty,
                    WordType = r.Vocabulary != null ? r.Vocabulary.WordType : null,
                    ExampleSentence = r.Vocabulary != null ? r.Vocabulary.ExampleSentence : null,
                    ExamplePinyin = r.Vocabulary != null ? r.Vocabulary.ExamplePinyin : null,
                    ExampleMeaning = r.Vocabulary != null ? r.Vocabulary.ExampleMeaning : null,
                    AudioUrl = r.Vocabulary != null ? r.Vocabulary.AudioUrl : null,
                    Source = r.Source,
                    AddedAt = r.AddedAt
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(list);
        });

        app.MapPost("/api/hsk/review/batch", [Microsoft.AspNetCore.Authorization.Authorize] async (
                Backend.Application.DTOs.AddHskReviewWordsRequest req,
                System.Security.Claims.ClaimsPrincipal user,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            if (req.VocabularyIds == null || req.VocabularyIds.Count == 0)
            {
                var total = await dbContext.HskReviewWords.CountAsync(r => r.UserId == userId, cancellationToken);
                return Results.Ok(new Backend.Application.DTOs.AddHskReviewWordsResponse(0, total, "Không có từ vựng nào được gửi."));
            }

            var validIds = (await dbContext.HskVocabularies
                .Where(v => req.VocabularyIds.Contains(v.Id))
                .Select(v => v.Id)
                .ToListAsync(cancellationToken)).ToHashSet();

            var existingIds = (await dbContext.HskReviewWords
                .Where(r => r.UserId == userId && req.VocabularyIds.Contains(r.VocabularyId))
                .Select(r => r.VocabularyId)
                .ToListAsync(cancellationToken)).ToHashSet();

            var toAddIds = validIds.Except(existingIds).ToList();
            if (toAddIds.Count > 0)
            {
                var entities = toAddIds.Select(id => new Backend.Domain.Entities.HskReviewWord
                {
                    UserId = userId,
                    VocabularyId = id,
                    AddedAt = DateTime.UtcNow,
                    Source = string.IsNullOrWhiteSpace(req.Source) ? "HskShooter" : req.Source
                }).ToList();

                dbContext.HskReviewWords.AddRange(entities);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var totalCount = await dbContext.HskReviewWords.CountAsync(r => r.UserId == userId, cancellationToken);
            return Results.Ok(new Backend.Application.DTOs.AddHskReviewWordsResponse(
                toAddIds.Count,
                totalCount,
                toAddIds.Count > 0 ? $"Đã lưu {toAddIds.Count} từ vào danh sách ôn tập." : "Các từ này đã có sẵn trong danh sách ôn tập."
            ));
        }).RequireRateLimiting("user-mutation");

        app.MapPost("/api/hsk/review/{vocabularyId:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (
                int vocabularyId,
                System.Security.Claims.ClaimsPrincipal user,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            bool exists = await dbContext.HskReviewWords.AnyAsync(r => r.UserId == userId && r.VocabularyId == vocabularyId, cancellationToken);
            if (!exists)
            {
                bool vocabExists = await dbContext.HskVocabularies.AnyAsync(v => v.Id == vocabularyId, cancellationToken);
                if (!vocabExists) return Results.NotFound("Không tìm thấy từ vựng.");

                dbContext.HskReviewWords.Add(new Backend.Domain.Entities.HskReviewWord
                {
                    UserId = userId,
                    VocabularyId = vocabularyId,
                    AddedAt = DateTime.UtcNow,
                    Source = "Manual"
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(new { vocabularyId, saved = true });
        }).RequireRateLimiting("user-mutation");

        app.MapDelete("/api/hsk/review/{vocabularyId:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (
                int vocabularyId,
                System.Security.Claims.ClaimsPrincipal user,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var rows = await dbContext.HskReviewWords
                .Where(r => r.UserId == userId && r.VocabularyId == vocabularyId)
                .ToListAsync(cancellationToken);

            if (rows.Count > 0)
            {
                dbContext.HskReviewWords.RemoveRange(rows);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(new { vocabularyId, removed = true });
        }).RequireRateLimiting("user-mutation");

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


        // ─── HSK: Vocabulary Excel/CSV Import (Single File) ───

        app.MapPost("/api/hsk/vocab/import-excel", async (Microsoft.AspNetCore.Http.IFormFile file,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                Backend.Application.Abstractions.IR2StorageService r2Storage,
                ICacheService cacheService,
                HttpContext httpContext,
                ILogger<Program> logger,
                CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("File không hợp lệ hoặc trống.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".csv" && ext != ".cvs" && ext != ".txt" && ext != ".tsv")
                return Results.BadRequest($"Vui lòng upload file Excel (.xlsx), CSV (.csv) hoặc TSV/TXT. File nhận được: {file.FileName}");

            var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
                       modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
                ? "upsert"
                : "skip";

            logger.LogInformation("[HSK Import] Bắt đầu nhận file: {FileName}, dung lượng: {Length} bytes, mode: {Mode}", file.FileName, file.Length, mode);

            List<List<string>> rawRows = new();
            if (ext == ".xlsx")
            {
                using var stream = file.OpenReadStream();
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                for (int r = 1; r <= lastRow; r++)
                {
                    var rowList = new List<string>();
                    for (int c = 1; c <= lastCol; c++)
                    {
                        rowList.Add(worksheet.Cell(r, c).GetString()?.Trim() ?? "");
                    }
                    rawRows.Add(rowList);
                }
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = await reader.ReadToEndAsync(cancellationToken);
                rawRows = HskVocabCsvParser.Parse(text).ToList();
            }

            var (parsedRows, parseLogs) = HskVocabCsvParser.ProcessRows(rawRows, file.FileName, logger);

            int success = 0, fail = 0, duplicate = 0, updated = 0;
            var errors = new List<string>();
            var jsonItems = new List<object>();
            var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var existingVocabs = await dbContext.HskVocabularies.ToListAsync(cancellationToken);
            var existingDict = existingVocabs.ToDictionary(v => $"{v.HskLevel}|{v.Hanzi}", v => v, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < parsedRows.Count; i++)
            {
                var row = parsedRows[i];
                try
                {
                    var key = $"{row.Level}|{row.Hanzi}";
                    if (!seenInFile.Add(key))
                    {
                        duplicate++;
                        continue;
                    }

                    var jsonItem = new
                    {
                        hskLevel = row.Level,
                        hanzi = row.Hanzi,
                        pinyin = row.Pinyin,
                        meaning = row.Meaning,
                        wordType = row.WordType,
                        exampleSentence = row.ExampleSentence,
                        examplePinyin = row.ExamplePinyin,
                        exampleMeaning = row.ExampleMeaning,
                        audioUrl = row.AudioUrl,
                        displayOrder = row.DisplayOrder,
                        topic = row.Topic
                    };

                    if (existingDict.TryGetValue(key, out var existing))
                    {
                        if (mode == "upsert")
                        {
                            if (existing.Pinyin == row.Pinyin &&
                                existing.Meaning == row.Meaning &&
                                existing.WordType == row.WordType &&
                                existing.ExampleSentence == row.ExampleSentence &&
                                existing.ExamplePinyin == row.ExamplePinyin &&
                                existing.ExampleMeaning == row.ExampleMeaning &&
                                existing.AudioUrl == row.AudioUrl &&
                                existing.DisplayOrder == row.DisplayOrder)
                            {
                                duplicate++;
                                continue;
                            }

                            existing.Pinyin = row.Pinyin;
                            existing.Meaning = row.Meaning;
                            existing.WordType = row.WordType;
                            existing.ExampleSentence = row.ExampleSentence;
                            existing.ExamplePinyin = row.ExamplePinyin;
                            existing.ExampleMeaning = row.ExampleMeaning;
                            if (!string.IsNullOrEmpty(row.AudioUrl)) existing.AudioUrl = row.AudioUrl;
                            existing.DisplayOrder = row.DisplayOrder;
                            updated++;
                            jsonItems.Add(jsonItem);
                        }
                        else
                        {
                            duplicate++;
                            continue;
                        }
                    }
                    else
                    {
                        var vocab = new Backend.Domain.Entities.HskVocabulary
                        {
                            HskLevel = row.Level,
                            Hanzi = row.Hanzi,
                            Pinyin = row.Pinyin,
                            Meaning = row.Meaning,
                            WordType = row.WordType,
                            ExampleSentence = row.ExampleSentence,
                            ExamplePinyin = row.ExamplePinyin,
                            ExampleMeaning = row.ExampleMeaning,
                            AudioUrl = row.AudioUrl,
                            DisplayOrder = row.DisplayOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.HskVocabularies.Add(vocab);
                        existingDict[key] = vocab;
                        success++;
                        jsonItems.Add(jsonItem);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {i + 1} ({row.Hanzi}): {ex.Message}");
                    fail++;
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[HSK Import] Lỗi lưu database");
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
                TotalRows = parsedRows.Count,
                ImportedCount = success,
                UpdatedCount = updated,
                DuplicateCount = duplicate,
                FailedCount = fail
            };
            dbContext.HskVocabularyImports.Add(batch);
            await dbContext.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

            logger.LogInformation("[HSK Import] Thành công file đơn {FileName}: Success={Success}, Updated={Updated}, Duplicate={Duplicate}, Fail={Fail}",
                file.FileName, success, updated, duplicate, fail);

            return Results.Ok(new { 
                Success = success, 
                Fail = fail, 
                Duplicate = duplicate, 
                Updated = updated, 
                Errors = errors, 
                JsonUrl = jsonUrl,
                Logs = parseLogs 
            });
        }).RequireAuthorization(policy => policy.RequireRole("admin")).DisableAntiforgery();

        // ─── HSK: Vocabulary Excel/CSV Import (Multiple Files) ───

        app.MapPost("/api/hsk/vocab/import-multiple", async (Microsoft.AspNetCore.Http.IFormFileCollection files,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                Backend.Application.Abstractions.IR2StorageService r2Storage,
                ICacheService cacheService,
                HttpContext httpContext,
                ILogger<Program> logger,
                CancellationToken cancellationToken) =>
        {
            if (files == null || files.Count == 0)
                return Results.BadRequest("Không có file nào được upload.");

            var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
                       modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
                ? "upsert"
                : "skip";

            logger.LogInformation("[HSK Import Multi] Bắt đầu nhận {Count} files, mode: {Mode}", files.Count, mode);

            var allParsedRows = new List<(HskParsedRow row, string fileName)>();
            var allLogs = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".csv" && ext != ".cvs" && ext != ".txt" && ext != ".tsv")
                {
                    allLogs.Add($"[Bỏ qua] File '{file.FileName}' không thuộc định dạng được hỗ trợ (.xlsx, .csv, .tsv, .txt).");
                    continue;
                }

                List<List<string>> rawRows = new();
                if (ext == ".xlsx")
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                    for (int r = 1; r <= lastRow; r++)
                    {
                        var rowList = new List<string>();
                        for (int c = 1; c <= lastCol; c++)
                        {
                            rowList.Add(worksheet.Cell(r, c).GetString()?.Trim() ?? "");
                        }
                        rawRows.Add(rowList);
                    }
                }
                else
                {
                    using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var text = await reader.ReadToEndAsync(cancellationToken);
                    rawRows = HskVocabCsvParser.Parse(text).ToList();
                }

                var (pRows, fLogs) = HskVocabCsvParser.ProcessRows(rawRows, file.FileName, logger);
                allLogs.AddRange(fLogs);
                foreach (var r in pRows)
                {
                    allParsedRows.Add((r, file.FileName));
                }
            }

            var existingVocabs = await dbContext.HskVocabularies.ToListAsync(cancellationToken);
            var existingDict = existingVocabs.ToDictionary(v => $"{v.HskLevel}|{v.Hanzi}", v => v, StringComparer.OrdinalIgnoreCase);

            int success = 0, fail = 0, duplicate = 0, updated = 0;
            var errors = new List<string>();
            var jsonItems = new List<object>();
            var seenInImport = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allParsedRows.Count; i++)
            {
                var (row, fileName) = allParsedRows[i];
                try
                {
                    var key = $"{row.Level}|{row.Hanzi}";
                    if (!seenInImport.Add(key))
                    {
                        duplicate++;
                        continue;
                    }

                    var jsonItem = new
                    {
                        hskLevel = row.Level,
                        hanzi = row.Hanzi,
                        pinyin = row.Pinyin,
                        meaning = row.Meaning,
                        wordType = row.WordType,
                        exampleSentence = row.ExampleSentence,
                        examplePinyin = row.ExamplePinyin,
                        exampleMeaning = row.ExampleMeaning,
                        audioUrl = row.AudioUrl,
                        displayOrder = row.DisplayOrder,
                        topic = row.Topic
                    };

                    if (existingDict.TryGetValue(key, out var existing))
                    {
                        if (mode == "upsert")
                        {
                            if (existing.Pinyin == row.Pinyin &&
                                existing.Meaning == row.Meaning &&
                                existing.WordType == row.WordType &&
                                existing.ExampleSentence == row.ExampleSentence &&
                                existing.ExamplePinyin == row.ExamplePinyin &&
                                existing.ExampleMeaning == row.ExampleMeaning &&
                                existing.AudioUrl == row.AudioUrl &&
                                existing.DisplayOrder == row.DisplayOrder)
                            {
                                duplicate++;
                            }
                            else
                            {
                                existing.Pinyin = row.Pinyin;
                                existing.Meaning = row.Meaning;
                                existing.WordType = row.WordType;
                                existing.ExampleSentence = row.ExampleSentence;
                                existing.ExamplePinyin = row.ExamplePinyin;
                                existing.ExampleMeaning = row.ExampleMeaning;
                                if (!string.IsNullOrEmpty(row.AudioUrl)) existing.AudioUrl = row.AudioUrl;
                                existing.DisplayOrder = row.DisplayOrder;
                                updated++;
                                jsonItems.Add(jsonItem);
                            }
                        }
                        else
                        {
                            duplicate++;
                        }
                    }
                    else
                    {
                        var entity = new Backend.Domain.Entities.HskVocabulary
                        {
                            HskLevel = row.Level,
                            Hanzi = row.Hanzi,
                            Pinyin = row.Pinyin,
                            Meaning = row.Meaning,
                            WordType = row.WordType,
                            ExampleSentence = row.ExampleSentence,
                            ExamplePinyin = row.ExamplePinyin,
                            ExampleMeaning = row.ExampleMeaning,
                            AudioUrl = row.AudioUrl,
                            DisplayOrder = row.DisplayOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.HskVocabularies.Add(entity);
                        existingDict[key] = entity;
                        success++;
                        jsonItems.Add(jsonItem);
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    errors.Add($"[{fileName} dòng {i + 1}] ({row.Hanzi}): {ex.Message}");
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[HSK Import Multi] Lỗi lưu database");
                return Results.Problem($"Lỗi lưu database: {ex.Message}");
            }

            string? jsonUrl = null;
            if (jsonItems.Count > 0)
            {
                var fileId = Guid.NewGuid().ToString("N")[..8];
                var vocabJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    importedAt = DateTime.UtcNow,
                    fileCount = files.Count,
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
                    jsonUrl = await r2Storage.UploadFileAsync(ms, $"hsk-vocab/multi_{fileId}.json", "application/json", cancellationToken);
                }
                catch
                {
                    var dir = Path.Combine("wwwroot", "exports");
                    Directory.CreateDirectory(dir);
                    await File.WriteAllBytesAsync(Path.Combine(dir, $"hsk-vocab_multi_{fileId}.json"), jsonBytes, cancellationToken);
                    jsonUrl = $"/exports/hsk-vocab_multi_{fileId}.json";
                }
            }

            var batch = new Backend.Domain.Entities.HskVocabularyImport
            {
                FileName = $"Batch ({files.Count} files): " + string.Join(", ", files.Take(3).Select(f => f.FileName)),
                JsonUrl = jsonUrl ?? string.Empty,
                TotalRows = allParsedRows.Count,
                ImportedCount = success,
                UpdatedCount = updated,
                DuplicateCount = duplicate,
                FailedCount = fail
            };
            dbContext.HskVocabularyImports.Add(batch);
            await dbContext.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveByPrefixAsync("hsk:vocab:", cancellationToken);

            logger.LogInformation("[HSK Import Multi] Hoàn tất {FileCount} file(s): Success={Success}, Updated={Updated}, Duplicate={Duplicate}, Fail={Fail}",
                files.Count, success, updated, duplicate, fail);

            return Results.Ok(new { 
                Success = success, 
                Fail = fail, 
                Duplicate = duplicate, 
                Updated = updated, 
                Errors = errors, 
                JsonUrl = jsonUrl,
                Logs = allLogs 
            });
        }).RequireAuthorization(policy => policy.RequireRole("admin")).DisableAntiforgery();

        // ─── Culture Stories & Bilingual Reading Endpoints ───

        app.MapGet("/api/hsk/culture-stories", async (
            string? category,
            string? level,
            string? search,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            var cacheKey = $"hsk:culture-stories:list:{category?.ToLowerInvariant()}:{level?.ToLowerInvariant()}:{search?.ToLowerInvariant()}";
            var cached = await cacheService.GetAsync<List<CultureStoryListItemDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var query = dbContext.CultureStories.Where(s => s.IsPublished).AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "Tất cả")
            {
                var catNorm = category.Trim().ToLowerInvariant();
                query = query.Where(s => s.Category.ToLower() == catNorm);
            }

            if (!string.IsNullOrWhiteSpace(level) && level != "All" && level != "Tất cả")
            {
                var lvlNorm = level.Trim().ToLowerInvariant();
                query = query.Where(s => s.Level.ToLower() == lvlNorm);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(st =>
                    st.TitleZh.ToLower().Contains(s) ||
                    st.TitleVi.ToLower().Contains(s) ||
                    st.Pinyin.ToLower().Contains(s) ||
                    (st.SummaryVi != null && st.SummaryVi.ToLower().Contains(s)));
            }

            var items = await query.OrderByDescending(st => st.CreatedAt).ToListAsync(cancellationToken);

            var dtos = items.Select(st => new CultureStoryListItemDto
            {
                Id = st.Id,
                TitleZh = st.TitleZh,
                TitleVi = st.TitleVi,
                Pinyin = st.Pinyin,
                Slug = st.Slug,
                Category = st.Category,
                Level = st.Level,
                SummaryVi = st.SummaryVi,
                AudioUrl = st.AudioUrl,
                ThumbnailUrl = st.ThumbnailUrl,
                EstimatedMinutes = st.EstimatedMinutes,
                ViewsCount = st.ViewsCount,
                IsPublished = st.IsPublished,
                CreatedAt = st.CreatedAt
            }).ToList();

            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1), cancellationToken);
            return Results.Ok(dtos);
        });

        app.MapGet("/api/hsk/culture-stories/{idOrSlug}", async (
            string idOrSlug,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            CultureStory? story = null;
            if (int.TryParse(idOrSlug, out int id))
            {
                story = await dbContext.CultureStories.FindAsync(new object[] { id }, cancellationToken);
            }
            else
            {
                var normSlug = idOrSlug.Trim().ToLowerInvariant();
                story = await dbContext.CultureStories.FirstOrDefaultAsync(s => s.Slug.ToLower() == normSlug, cancellationToken);
            }

            if (story == null) return Results.NotFound(new { Message = "Không tìm thấy bài đọc văn hóa." });

            story.ViewsCount++;
            await dbContext.SaveChangesAsync(cancellationToken);

            var dto = new CultureStoryDetailDto
            {
                Id = story.Id,
                TitleZh = story.TitleZh,
                TitleVi = story.TitleVi,
                Pinyin = story.Pinyin,
                Slug = story.Slug,
                Category = story.Category,
                Level = story.Level,
                SummaryVi = story.SummaryVi,
                ContentZh = story.ContentZh,
                ContentVi = story.ContentVi,
                ParagraphsJson = story.ParagraphsJson,
                VocabularyJson = story.VocabularyJson,
                QuestionsJson = story.QuestionsJson,
                AudioUrl = story.AudioUrl,
                ThumbnailUrl = story.ThumbnailUrl,
                EstimatedMinutes = story.EstimatedMinutes,
                ViewsCount = story.ViewsCount,
                IsPublished = story.IsPublished,
                CreatedAt = story.CreatedAt,
                UpdatedAt = story.UpdatedAt
            };

            return Results.Ok(dto);
        });

        app.MapPost("/api/hsk/culture-stories/{id:int}/quiz", async (
            int id,
            CultureStoryQuizSubmissionRequest req,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var story = await dbContext.CultureStories.FindAsync(new object[] { id }, cancellationToken);
            if (story == null) return Results.NotFound(new { Message = "Không tìm thấy bài đọc văn hóa." });

            var result = new CultureStoryQuizResultDto { StoryId = id };

            try
            {
                using var doc = JsonDocument.Parse(story.QuestionsJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    result.TotalQuestions = root.GetArrayLength();
                    int correctCount = 0;
                    for (int i = 0; i < root.GetArrayLength(); i++)
                    {
                        var q = root[i];
                        int correctIndex = q.TryGetProperty("correctIndex", out var cp) ? cp.GetInt32() : 0;
                        string explanation = q.TryGetProperty("explanation", out var ep) ? ep.GetString() ?? "" : "";

                        result.CorrectIndices.Add(correctIndex);
                        result.Explanations.Add(explanation);

                        bool isCorrect = (i < req.Answers.Count && req.Answers[i] == correctIndex);
                        result.AnswerCorrectness.Add(isCorrect);
                        if (isCorrect) correctCount++;
                    }

                    result.CorrectCount = correctCount;
                    result.ScorePercentage = result.TotalQuestions > 0
                        ? Math.Round((double)correctCount / result.TotalQuestions * 100, 1)
                        : 100;
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { Message = "Lỗi chấm điểm bài tập đọc hiểu: " + ex.Message });
            }

            return Results.Ok(result);
        });

        app.MapPost("/api/admin/hsk/culture-stories",
            [Authorize(Roles = "admin")] async (
                CreateCultureStoryRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(req.TitleZh) || string.IsNullOrWhiteSpace(req.TitleVi))
                    return Results.BadRequest(new { Message = "Tiêu đề tiếng Trung và tiếng Việt không được để trống." });

                var pinyin = string.IsNullOrWhiteSpace(req.Pinyin) ? req.TitleZh.Trim() : req.Pinyin.Trim();

                var slug = string.IsNullOrWhiteSpace(req.Slug)
                    ? Regex.Replace(req.TitleVi.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-")
                    : req.Slug.Trim();

                if (string.IsNullOrWhiteSpace(slug)) slug = "story-" + Guid.NewGuid().ToString()[..8];

                int suffix = 1;
                var baseSlug = slug;
                while (await dbContext.CultureStories.AnyAsync(s => s.Slug == slug, cancellationToken))
                {
                    slug = $"{baseSlug}-{suffix++}";
                }

                var entity = new CultureStory
                {
                    TitleZh = req.TitleZh.Trim(),
                    TitleVi = req.TitleVi.Trim(),
                    Pinyin = pinyin,
                    Slug = slug,
                    Category = string.IsNullOrWhiteSpace(req.Category) ? "chengyu" : req.Category.Trim(),
                    Level = string.IsNullOrWhiteSpace(req.Level) ? "HSK 3" : req.Level.Trim(),
                    SummaryVi = req.SummaryVi?.Trim(),
                    ContentZh = req.ContentZh ?? string.Empty,
                    ContentVi = req.ContentVi ?? string.Empty,
                    ParagraphsJson = string.IsNullOrWhiteSpace(req.ParagraphsJson) ? "[]" : req.ParagraphsJson,
                    VocabularyJson = string.IsNullOrWhiteSpace(req.VocabularyJson) ? "[]" : req.VocabularyJson,
                    QuestionsJson = string.IsNullOrWhiteSpace(req.QuestionsJson) ? "[]" : req.QuestionsJson,
                    AudioUrl = req.AudioUrl?.Trim(),
                    ThumbnailUrl = req.ThumbnailUrl?.Trim(),
                    EstimatedMinutes = req.EstimatedMinutes ?? 3,
                    IsPublished = req.IsPublished ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.CultureStories.Add(entity);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("hsk:culture-stories:", cancellationToken);

                return Results.Created($"/api/hsk/culture-stories/{entity.Id}", new { Id = entity.Id, Slug = entity.Slug, Message = "Tạo bài đọc văn hóa thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPut("/api/admin/hsk/culture-stories/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                UpdateCultureStoryRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var story = await dbContext.CultureStories.FindAsync(new object[] { id }, cancellationToken);
                if (story == null) return Results.NotFound(new { Message = "Không tìm thấy bài đọc văn hóa để cập nhật." });

                if (!string.IsNullOrWhiteSpace(req.TitleZh)) story.TitleZh = req.TitleZh.Trim();
                if (!string.IsNullOrWhiteSpace(req.TitleVi)) story.TitleVi = req.TitleVi.Trim();
                if (!string.IsNullOrWhiteSpace(req.Pinyin)) story.Pinyin = req.Pinyin.Trim();
                if (!string.IsNullOrWhiteSpace(req.Category)) story.Category = req.Category.Trim();
                if (!string.IsNullOrWhiteSpace(req.Level)) story.Level = req.Level.Trim();
                story.SummaryVi = req.SummaryVi?.Trim();
                if (req.ContentZh != null) story.ContentZh = req.ContentZh;
                if (req.ContentVi != null) story.ContentVi = req.ContentVi;
                if (!string.IsNullOrWhiteSpace(req.ParagraphsJson)) story.ParagraphsJson = req.ParagraphsJson;
                if (!string.IsNullOrWhiteSpace(req.VocabularyJson)) story.VocabularyJson = req.VocabularyJson;
                if (!string.IsNullOrWhiteSpace(req.QuestionsJson)) story.QuestionsJson = req.QuestionsJson;
                story.AudioUrl = req.AudioUrl?.Trim();
                story.ThumbnailUrl = req.ThumbnailUrl?.Trim();
                if (req.EstimatedMinutes.HasValue) story.EstimatedMinutes = req.EstimatedMinutes.Value;
                if (req.IsPublished.HasValue) story.IsPublished = req.IsPublished.Value;
                story.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await cacheService.RemoveByPrefixAsync("hsk:culture-stories:", cancellationToken);

                return Results.Ok(new { Id = story.Id, Message = "Cập nhật bài đọc văn hóa thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapDelete("/api/admin/hsk/culture-stories/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var story = await dbContext.CultureStories.FindAsync(new object[] { id }, cancellationToken);
                if (story == null) return Results.NotFound(new { Message = "Không tìm thấy bài đọc văn hóa để xóa." });

                dbContext.CultureStories.Remove(story);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("hsk:culture-stories:", cancellationToken);
                return Results.Ok(new { Message = "Đã xóa bài đọc văn hóa thành công." });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPost("/api/admin/hsk/culture-stories/import-json",
            [Authorize(Roles = "admin")] async (
                ImportCultureStoryJsonRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(req.JsonContent))
                    return Results.BadRequest(new { Message = "Nội dung JSON không được để trống." });

                try
                {
                    using var doc = JsonDocument.Parse(req.JsonContent);
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        return Results.BadRequest(new { Message = "JSON phải là một mảng danh sách bài đọc văn hóa." });

                    var importedList = new List<CultureStory>();
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var titleZh = elem.TryGetProperty("titleZh", out var tzp) ? tzp.GetString() ?? "" : "";
                        var titleVi = elem.TryGetProperty("titleVi", out var tvp) ? tvp.GetString() ?? "" : "";
                        var pinyin = elem.TryGetProperty("pinyin", out var pyp) ? pyp.GetString() ?? "" : "";
                        var slug = elem.TryGetProperty("slug", out var slp) ? slp.GetString() : null;
                        if (string.IsNullOrWhiteSpace(slug))
                        {
                            slug = Regex.Replace(titleVi.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-");
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

                        string questionsJson = "[]";
                        if (elem.TryGetProperty("questions", out var qsp))
                            questionsJson = qsp.GetRawText();

                        var audioUrl = elem.TryGetProperty("audioUrl", out var aup) ? aup.GetString() : null;
                        var thumbUrl = elem.TryGetProperty("thumbnailUrl", out var tmp) ? tmp.GetString() : null;
                        var estMins = elem.TryGetProperty("estimatedMinutes", out var emp) ? emp.GetInt32() : 3;

                        importedList.Add(new CultureStory
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

                    if (importedList.Count > 0)
                    {
                        dbContext.CultureStories.AddRange(importedList);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await cacheService.RemoveByPrefixAsync("hsk:culture-stories:", cancellationToken);
                    }

                    return Results.Ok(new { Imported = importedList.Count, Message = $"Đã nhập thành công {importedList.Count} bài đọc văn hóa." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = "Lỗi xử lý JSON bài đọc văn hóa: " + ex.Message });
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        // ─── GenZ Slang & Number Ciphers Endpoints ───

        app.MapGet("/api/hsk/slang", async (
            string? category,
            string? search,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            var cacheKey = $"hsk:slang:list:{category?.ToLowerInvariant()}:{search?.ToLowerInvariant()}";
            var cached = await cacheService.GetAsync<List<SlangVocabDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var query = dbContext.SlangVocabs.Where(s => s.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "Tất cả")
            {
                var catNorm = category.Trim().ToLowerInvariant();
                query = query.Where(s => s.Category.ToLower() == catNorm);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(sv =>
                    sv.Term.ToLower().Contains(s) ||
                    sv.Pinyin.ToLower().Contains(s) ||
                    sv.MeaningVi.ToLower().Contains(s) ||
                    (sv.Origin != null && sv.Origin.ToLower().Contains(s)));
            }

            var items = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.PopularityScore)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(s => new SlangVocabDto
            {
                Id = s.Id,
                Term = s.Term,
                Pinyin = s.Pinyin,
                MeaningVi = s.MeaningVi,
                Category = s.Category,
                Origin = s.Origin,
                DialogueExampleZh = s.DialogueExampleZh,
                DialogueExampleVi = s.DialogueExampleVi,
                AudioUrl = s.AudioUrl,
                PopularityScore = s.PopularityScore,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1), cancellationToken);
            return Results.Ok(dtos);
        });

        app.MapGet("/api/hsk/slang/{id:int}", async (
            int id,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var item = await dbContext.SlangVocabs.FindAsync(new object[] { id }, cancellationToken);
            if (item == null) return Results.NotFound(new { Message = "Không tìm thấy từ lóng / mật mã." });

            var dto = new SlangVocabDto
            {
                Id = item.Id,
                Term = item.Term,
                Pinyin = item.Pinyin,
                MeaningVi = item.MeaningVi,
                Category = item.Category,
                Origin = item.Origin,
                DialogueExampleZh = item.DialogueExampleZh,
                DialogueExampleVi = item.DialogueExampleVi,
                AudioUrl = item.AudioUrl,
                PopularityScore = item.PopularityScore,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };

            return Results.Ok(dto);
        });

        app.MapPost("/api/admin/hsk/slang",
            [Authorize(Roles = "admin")] async (
                CreateSlangVocabRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(req.Term) || string.IsNullOrWhiteSpace(req.MeaningVi))
                    return Results.BadRequest(new { Message = "Thuật ngữ và ý nghĩa tiếng Việt không được để trống." });

                var term = req.Term.Trim();
                var exists = await dbContext.SlangVocabs.AnyAsync(s => s.Term.ToLower() == term.ToLower(), cancellationToken);
                if (exists) return Results.BadRequest(new { Message = "Thuật ngữ / Mật mã này đã tồn tại." });

                var entity = new SlangVocab
                {
                    Term = term,
                    Pinyin = string.IsNullOrWhiteSpace(req.Pinyin) ? term : req.Pinyin.Trim(),
                    MeaningVi = req.MeaningVi.Trim(),
                    Category = string.IsNullOrWhiteSpace(req.Category) ? "slang_trend" : req.Category.Trim(),
                    Origin = req.Origin?.Trim(),
                    DialogueExampleZh = req.DialogueExampleZh?.Trim(),
                    DialogueExampleVi = req.DialogueExampleVi?.Trim(),
                    AudioUrl = req.AudioUrl?.Trim(),
                    PopularityScore = req.PopularityScore ?? 90,
                    DisplayOrder = req.DisplayOrder ?? 0,
                    IsActive = req.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.SlangVocabs.Add(entity);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("hsk:slang:", cancellationToken);

                return Results.Created($"/api/hsk/slang/{entity.Id}", new { Id = entity.Id, Message = "Thêm từ lóng / mật mã thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPut("/api/admin/hsk/slang/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                UpdateSlangVocabRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var item = await dbContext.SlangVocabs.FindAsync(new object[] { id }, cancellationToken);
                if (item == null) return Results.NotFound(new { Message = "Không tìm thấy từ lóng / mật mã để cập nhật." });

                if (!string.IsNullOrWhiteSpace(req.Term))
                {
                    var term = req.Term.Trim();
                    if (term.ToLower() != item.Term.ToLower())
                    {
                        var exists = await dbContext.SlangVocabs.AnyAsync(s => s.Term.ToLower() == term.ToLower() && s.Id != id, cancellationToken);
                        if (exists) return Results.BadRequest(new { Message = "Thuật ngữ / Mật mã này đã tồn tại ở bản ghi khác." });
                    }
                    item.Term = term;
                }

                if (!string.IsNullOrWhiteSpace(req.Pinyin)) item.Pinyin = req.Pinyin.Trim();
                if (!string.IsNullOrWhiteSpace(req.MeaningVi)) item.MeaningVi = req.MeaningVi.Trim();
                if (!string.IsNullOrWhiteSpace(req.Category)) item.Category = req.Category.Trim();
                item.Origin = req.Origin?.Trim();
                item.DialogueExampleZh = req.DialogueExampleZh?.Trim();
                item.DialogueExampleVi = req.DialogueExampleVi?.Trim();
                item.AudioUrl = req.AudioUrl?.Trim();
                if (req.PopularityScore.HasValue) item.PopularityScore = req.PopularityScore.Value;
                if (req.DisplayOrder.HasValue) item.DisplayOrder = req.DisplayOrder.Value;
                if (req.IsActive.HasValue) item.IsActive = req.IsActive.Value;
                item.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await cacheService.RemoveByPrefixAsync("hsk:slang:", cancellationToken);

                return Results.Ok(new { Id = item.Id, Message = "Cập nhật từ lóng thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapDelete("/api/admin/hsk/slang/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var item = await dbContext.SlangVocabs.FindAsync(new object[] { id }, cancellationToken);
                if (item == null) return Results.NotFound(new { Message = "Không tìm thấy từ lóng để xóa." });

                dbContext.SlangVocabs.Remove(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("hsk:slang:", cancellationToken);
                return Results.Ok(new { Message = "Đã xóa từ lóng thành công." });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPost("/api/admin/hsk/slang/import-json",
            [Authorize(Roles = "admin")] async (
                ImportSlangVocabJsonRequest req,
                Backend.Infrastructure.Persistence.AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(req.JsonContent))
                    return Results.BadRequest(new { Message = "Nội dung JSON không được để trống." });

                try
                {
                    using var doc = JsonDocument.Parse(req.JsonContent);
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        return Results.BadRequest(new { Message = "JSON phải là một mảng danh sách từ lóng / mật mã." });

                    var importedList = new List<SlangVocab>();
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

                        importedList.Add(new SlangVocab
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

                    if (importedList.Count > 0)
                    {
                        dbContext.SlangVocabs.AddRange(importedList);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await cacheService.RemoveByPrefixAsync("hsk:slang:", cancellationToken);
                    }

                    return Results.Ok(new { Imported = importedList.Count, Message = $"Đã nhập thành công {importedList.Count} từ lóng / mật mã." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = "Lỗi xử lý JSON từ lóng: " + ex.Message });
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        // ─── HSK Idioms & WeChat Endpoints ───

        app.MapGet("/api/hsk/idioms", async (
            string? level,
            string? category,
            string? search,
            IWebHostEnvironment env,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            var cacheKey = $"hsk:idioms:list:{level?.ToLowerInvariant()}:{category?.ToLowerInvariant()}:{search?.ToLowerInvariant()}";
            var cached = await cacheService.GetAsync<List<ChengyuDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var filePath = Path.Combine(env.ContentRootPath, "..", "..", "frontend", "src", "Frontend.App", "wwwroot", "sample-data", "hsk-chengyu.json");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(AppContext.BaseDirectory, "sample-data", "hsk-chengyu.json");
            }

            if (!File.Exists(filePath)) return Results.Ok(new List<ChengyuDto>());

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var items = JsonSerializer.Deserialize<List<ChengyuDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            if (!string.IsNullOrWhiteSpace(level) && level != "all" && level != "All" && level != "Tất cả")
            {
                items = items.Where(x => x.HskLevel.Equals(level, StringComparison.OrdinalIgnoreCase) ||
                                         x.HskLevel.Replace(" ", "").Equals(level.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "all" && category != "All" && category != "Tất cả")
            {
                items = items.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                                         (x.CategoryVi != null && x.CategoryVi.Contains(category, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                items = items.Where(x =>
                    x.Hanzi.ToLowerInvariant().Contains(s) ||
                    x.Pinyin.ToLowerInvariant().Contains(s) ||
                    x.HanViet.ToLowerInvariant().Contains(s) ||
                    x.MeaningVi.ToLowerInvariant().Contains(s)).ToList();
            }

            await cacheService.SetAsync(cacheKey, items, TimeSpan.FromMinutes(60), cancellationToken);
            return Results.Ok(items);
        })
        .RequireRateLimiting("public-read");

        app.MapGet("/api/hsk/idioms/{id}", async (
            string id,
            IWebHostEnvironment env,
            CancellationToken cancellationToken) =>
        {
            var filePath = Path.Combine(env.ContentRootPath, "..", "..", "frontend", "src", "Frontend.App", "wwwroot", "sample-data", "hsk-chengyu.json");
            if (!File.Exists(filePath)) filePath = Path.Combine(AppContext.BaseDirectory, "sample-data", "hsk-chengyu.json");
            if (!File.Exists(filePath)) return Results.NotFound(new { Message = "Không tìm thấy thành ngữ." });

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var items = JsonSerializer.Deserialize<List<ChengyuDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            var found = items.FirstOrDefault(x => x.Id == id || x.Hanzi == id);
            return found != null ? Results.Ok(found) : Results.NotFound(new { Message = "Không tìm thấy thành ngữ." });
        })
        .RequireRateLimiting("public-read");

        app.MapPost("/api/admin/hsk/idioms",
            [Authorize(Roles = "admin")] async (
                ChengyuDto req,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                await cacheService.RemoveByPrefixAsync("hsk:idioms:", cancellationToken);
                return Results.Ok(new { Message = "Đã lưu thành ngữ thành công!", Item = req });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapDelete("/api/admin/hsk/idioms/{id}",
            [Authorize(Roles = "admin")] async (
                string id,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                await cacheService.RemoveByPrefixAsync("hsk:idioms:", cancellationToken);
                return Results.Ok(new { Message = "Đã xóa thành ngữ thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPost("/api/admin/hsk/idioms/sync-r2",
            [Authorize(Roles = "admin")] async (
                SyncChengyuR2Request? req,
                IR2StorageService r2Service,
                IWebHostEnvironment env,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var filePath = Path.Combine(env.ContentRootPath, "..", "..", "frontend", "src", "Frontend.App", "wwwroot", "sample-data", "hsk-chengyu.json");
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(AppContext.BaseDirectory, "sample-data", "hsk-chengyu.json");
                }

                string jsonContent;
                if (!string.IsNullOrWhiteSpace(req?.JsonContent))
                {
                    jsonContent = req.JsonContent;
                    try
                    {
                        var dir = Path.GetDirectoryName(filePath);
                        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        await File.WriteAllTextAsync(filePath, jsonContent, cancellationToken);
                    }
                    catch { }
                }
                else if (File.Exists(filePath))
                {
                    jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                }
                else
                {
                    return Results.BadRequest(new { Message = "Không tìm thấy dữ liệu JSON thành ngữ để đẩy lên R2." });
                }

                var count = 0;
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        count = doc.RootElement.GetArrayLength();
                    }
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = "Nội dung JSON không hợp lệ: " + ex.Message });
                }

                var r2Url = "";
                var uploaded = false;
                var fileName = "hsk/chengyu.json";
                var versionedName = $"hsk/chengyu_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

                try
                {
                    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    using var ms = new MemoryStream(jsonBytes);
                    r2Url = await r2Service.UploadFileAsync(ms, fileName, "application/json", cancellationToken);
                    uploaded = true;

                    // Also backup versioned snapshot
                    try
                    {
                        using var msVer = new MemoryStream(jsonBytes);
                        await r2Service.UploadFileAsync(msVer, versionedName, "application/json", cancellationToken);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    r2Url = $"[Dev Mode / Fallback] R2: {ex.Message}";
                }

                await cacheService.RemoveByPrefixAsync("hsk:idioms:", cancellationToken);

                return Results.Ok(new SyncChengyuR2Response
                {
                    Success = true,
                    Count = count,
                    R2Url = r2Url,
                    IsUploaded = uploaded,
                    FileName = fileName,
                    SizeKb = Math.Round((double)jsonContent.Length / 1024, 2),
                    SyncedAt = DateTime.UtcNow,
                    Message = uploaded 
                        ? $"Đã đẩy nhanh thành công {count} thành ngữ lên Cloudflare R2 ({fileName})!" 
                        : $"Đã lưu file JSON ({count} thành ngữ). {r2Url}"
                });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        return app;
    }
}
