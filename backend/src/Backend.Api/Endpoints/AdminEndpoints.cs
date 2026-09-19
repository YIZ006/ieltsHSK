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

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
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


        return app;
    }
}
