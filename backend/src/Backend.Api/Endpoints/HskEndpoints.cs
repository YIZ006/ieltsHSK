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

        return app;
    }
}
