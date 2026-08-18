using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        ToeicUrl = m.ToeicUrl
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

app.Run();

public record CreateExamRequest(string Title, string DataUrl, string Category = "IELTS");
public record SaveToeicExamRequest(string CollectionName, string Title, int? MockTestId, System.Text.Json.JsonElement ExamData);
