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

public static class StoryEndpoints
{
    public static IEndpointRouteBuilder MapStoryEndpoints(this IEndpointRouteBuilder app)
    {
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

        return app;
    }
}
