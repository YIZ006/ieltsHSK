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

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
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
        }).RequireRateLimiting("auth");


        app.MapGet("/api/auth/check-username", async (string? username, IAuthService authService, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(username)) return Results.Ok(new { isTaken = false });
            var isTaken = await authService.IsUsernameTakenAsync(username, cancellationToken);
            return Results.Ok(new { isTaken });
        }).RequireRateLimiting("auth");


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
        }).RequireRateLimiting("auth");


        app.MapPost("/api/admin/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await authService.AdminLoginAsync(request, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireRateLimiting("auth");


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
        }).RequireRateLimiting("auth");


        app.MapPost("/api/auth/google-register", async (GoogleLoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await authService.RegisterWithGoogleAsync(request, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireRateLimiting("auth");

        // Gia hạn phiên đăng nhập: đổi refresh token lấy access token + refresh token mới (rotation)

        app.MapPost("/api/auth/refresh", async (RefreshRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await authService.RefreshAsync(request, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireRateLimiting("auth");

        // Đăng xuất: thu hồi refresh token phía server

        app.MapPost("/api/auth/logout", async (RefreshRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            await authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Results.Ok(new { Message = "Logged out" });
        });


        app.MapGet("/api/user/me", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (dbUser != null)
                {
                    return Results.Ok(new
                    {
                        dbUser.Id,
                        dbUser.Username,
                        dbUser.UsernameChangedAt,
                        dbUser.FullName,
                        dbUser.Email,
                        dbUser.Role,
                        dbUser.Avatar,
                        dbUser.AvatarColor,
                        dbUser.Bio,
                        dbUser.TargetExam,
                        dbUser.TargetScore,
                        dbUser.TargetDeadline,
                        dbUser.IeltsLevel,
                        dbUser.HskLevel,
                        dbUser.Level,
                        dbUser.Streak,
                        dbUser.LastActive,
                        dbUser.CreatedAt
                    });
                }
            }
            return Results.Unauthorized();
        });


        app.MapPut("/api/user/profile", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateProfileRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            try
            {
                Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.FullName, "Họ và tên");
                Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Username, "Tên hiển thị");
                Backend.Application.Common.SecuritySanitizer.ValidateSafeText(request.Avatar, "Ảnh đại diện");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (dbUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.Username))
                    {
                        var newUsername = request.Username.Trim().ToLowerInvariant();
                        if (!System.Text.RegularExpressions.Regex.IsMatch(newUsername, @"^[a-z0-9._-]{3,30}$"))
                        {
                            return Results.BadRequest(new { message = "Tên hiển thị (username) chỉ được gồm chữ cái không dấu (a-z), số (0-9), dấu '.', '_', '-' và từ 3 đến 30 ký tự, không dùng tiếng Việt có dấu." });
                        }

                        if (!string.Equals(dbUser.Username, newUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            if (dbUser.UsernameChangedAt.HasValue)
                            {
                                var daysSinceChange = (DateTime.UtcNow - dbUser.UsernameChangedAt.Value).TotalDays;
                                if (daysSinceChange < 30)
                                {
                                    var daysLeft = Math.Max(1, (int)Math.Ceiling(30 - daysSinceChange));
                                    var nextAllowed = dbUser.UsernameChangedAt.Value.AddDays(30);
                                    return Results.BadRequest(new
                                    {
                                        message = $"Bạn chỉ có thể đổi tên hiển thị 30 ngày một lần. Vui lòng quay lại sau {daysLeft} ngày nữa (ngày {nextAllowed:dd/MM/yyyy}).",
                                        daysRemaining = daysLeft,
                                        nextAllowedAt = nextAllowed
                                    });
                                }
                            }

                            var isTaken = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                                dbContext.Users,
                                u => u.Id != userId && u.Username.ToLower() == newUsername,
                                cancellationToken);

                            if (isTaken)
                            {
                                return Results.BadRequest(new { message = "Tên hiển thị (username) này đã có người sử dụng. Vui lòng chọn tên khác." });
                            }
                            dbUser.Username = newUsername;
                            dbUser.UsernameChangedAt = DateTime.UtcNow;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(request.FullName)) dbUser.FullName = request.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(request.Avatar)) dbUser.Avatar = request.Avatar.Trim();
                    if (request.AvatarColor != null) dbUser.AvatarColor = request.AvatarColor.Trim();
                    if (request.Bio != null) dbUser.Bio = request.Bio.Trim();
                    if (request.TargetExam != null) dbUser.TargetExam = request.TargetExam.Trim();
                    if (request.TargetScore != null) dbUser.TargetScore = request.TargetScore.Trim();
                    if (request.TargetDeadline.HasValue) dbUser.TargetDeadline = DateTime.SpecifyKind(request.TargetDeadline.Value, DateTimeKind.Utc);
                    if (request.IeltsLevel != null) dbUser.IeltsLevel = request.IeltsLevel.Trim();
                    if (request.HskLevel != null) dbUser.HskLevel = request.HskLevel.Trim();
                    if (!string.IsNullOrWhiteSpace(request.Level)) dbUser.Level = request.Level.Trim();
                    dbUser.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return Results.Ok(new
                    {
                        dbUser.Id,
                        dbUser.Username,
                        dbUser.UsernameChangedAt,
                        dbUser.FullName,
                        dbUser.Avatar,
                        dbUser.AvatarColor,
                        dbUser.Bio,
                        dbUser.TargetExam,
                        dbUser.TargetScore,
                        dbUser.TargetDeadline,
                        dbUser.IeltsLevel,
                        dbUser.HskLevel,
                        dbUser.Level
                    });
                }
            }
            return Results.Unauthorized();
        });


        app.MapPost("/api/user/streak", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (dbUser != null)
                {
                    var today = DateTime.UtcNow.Date;
                    if (!dbUser.LastActive.HasValue || dbUser.LastActive.Value.Date != today)
                    {
                        if (dbUser.LastActive.HasValue && dbUser.LastActive.Value.Date == today.AddDays(-1))
                        {
                            dbUser.Streak += 1;
                        }
                        else if (!dbUser.LastActive.HasValue || dbUser.LastActive.Value.Date < today.AddDays(-1))
                        {
                            dbUser.Streak = 1;
                        }
                        dbUser.LastActive = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                    return Results.Ok(new { streak = dbUser.Streak, lastActive = dbUser.LastActive });
                }
            }
            return Results.Unauthorized();
        });


        app.MapPut("/api/user/level", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.UpdateLevelRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (dbUser != null)
                {
                    dbUser.Level = request.Level;
                    dbUser.IeltsLevel = request.Level;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return Results.Ok();
                }
            }
            return Results.Unauthorized();
        });

        // ==========================================
        // STUDY ACTIVITY & STREAK ENDPOINTS
        // ==========================================

        app.MapPost("/api/user/study-time", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.AddStudyTimeRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (dbUser == null) return Results.Unauthorized();

            if (request.Seconds <= 0)
            {
                return Results.BadRequest(new { message = "Seconds must be greater than 0" });
            }

            var targetDateUtc = DateTime.SpecifyKind((request.Date ?? DateTime.UtcNow).Date, DateTimeKind.Utc);

            var activity = await dbContext.UserStudyActivities
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == targetDateUtc, cancellationToken);

            if (activity == null)
            {
                activity = new Backend.Domain.Entities.UserStudyActivity
                {
                    UserId = userId,
                    ActivityDate = targetDateUtc,
                    StudySeconds = request.Seconds,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.UserStudyActivities.Add(activity);
            }
            else
            {
                activity.StudySeconds += request.Seconds;
                activity.UpdatedAt = DateTime.UtcNow;
            }

            // Calculate updated streak
            var allActivities = await dbContext.UserStudyActivities
                .Where(a => a.UserId == userId && a.StudySeconds > 0)
                .Select(a => a.ActivityDate)
                .ToListAsync(cancellationToken);

            if (!allActivities.Any(d => d.Date == targetDateUtc.Date))
            {
                allActivities.Add(targetDateUtc);
            }

            var activeDates = allActivities.Select(d => d.Date).Distinct().ToHashSet();
            var todayUtc = DateTime.UtcNow.Date;
            int currentStreak = 0;
            var checkDate = todayUtc;
            if (!activeDates.Contains(checkDate))
            {
                checkDate = todayUtc.AddDays(-1);
            }
            while (activeDates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }

            dbUser.Streak = currentStreak;
            dbUser.LastActive = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                streak = dbUser.Streak,
                todaySeconds = activity.StudySeconds
            });
        });


        app.MapGet("/api/user/study-activity", [Microsoft.AspNetCore.Authorization.Authorize] async (System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var activities = await dbContext.UserStudyActivities.AsNoTracking()
                .Where(a => a.UserId == userId && a.StudySeconds > 0)
                .OrderBy(a => a.ActivityDate)
                .ToListAsync(cancellationToken);

            var activeDates = activities.Select(a => a.ActivityDate.Date).Distinct().ToHashSet();
            var activeDays = activeDates.OrderBy(d => d).Select(d => d.ToString("yyyy-MM-dd")).ToList();

            int bestStreak = 0;
            int tempStreak = 0;
            DateTime? prev = null;
            foreach (var d in activeDates.OrderBy(d => d))
            {
                if (prev.HasValue && d == prev.Value.AddDays(1))
                {
                    tempStreak++;
                }
                else
                {
                    tempStreak = 1;
                }
                if (tempStreak > bestStreak) bestStreak = tempStreak;
                prev = d;
            }

            var todayUtc = DateTime.UtcNow.Date;
            int currentStreak = 0;
            var checkDate = todayUtc;
            if (!activeDates.Contains(checkDate))
            {
                checkDate = todayUtc.AddDays(-1);
            }
            while (activeDates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }

            var todayActivity = activities.FirstOrDefault(a => a.ActivityDate.Date == todayUtc);
            int todaySeconds = todayActivity?.StudySeconds ?? 0;

            var dailyList = activities.Select(a => new Backend.Application.DTOs.DailyStudyItemDto(a.ActivityDate, a.StudySeconds)).ToList();

            return Results.Ok(new Backend.Application.DTOs.StudyActivityResponseDto(
                currentStreak,
                bestStreak,
                todaySeconds,
                dailyList,
                activeDays
            ));
        });


        app.MapPost("/api/user/study-activity/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.MigrateStudyActivityRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var dbUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (dbUser == null) return Results.Unauthorized();

            int migratedCount = 0;

            if (request.DailySeconds != null)
            {
                foreach (var (dateStr, sec) in request.DailySeconds)
                {
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                    {
                        var dateUtc = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
                        var act = await dbContext.UserStudyActivities
                            .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == dateUtc, cancellationToken);
                        if (act == null)
                        {
                            dbContext.UserStudyActivities.Add(new Backend.Domain.Entities.UserStudyActivity
                            {
                                UserId = userId,
                                ActivityDate = dateUtc,
                                StudySeconds = sec,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                        else if (sec > act.StudySeconds)
                        {
                            act.StudySeconds = sec;
                            act.UpdatedAt = DateTime.UtcNow;
                        }
                        migratedCount++;
                    }
                }
            }

            if (request.ActiveDays != null)
            {
                foreach (var dateStr in request.ActiveDays)
                {
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                    {
                        var dateUtc = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
                        var act = await dbContext.UserStudyActivities
                            .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityDate == dateUtc, cancellationToken);
                        if (act == null)
                        {
                            dbContext.UserStudyActivities.Add(new Backend.Domain.Entities.UserStudyActivity
                            {
                                UserId = userId,
                                ActivityDate = dateUtc,
                                StudySeconds = 60, // ensure at least 1 minute to count as active
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                            migratedCount++;
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Recalculate streak
            var allActivities = await dbContext.UserStudyActivities
                .Where(a => a.UserId == userId && a.StudySeconds > 0)
                .Select(a => a.ActivityDate)
                .ToListAsync(cancellationToken);

            var activeDates = allActivities.Select(d => d.Date).Distinct().ToHashSet();
            var todayUtc = DateTime.UtcNow.Date;
            int currentStreak = 0;
            var checkDate = todayUtc;
            if (!activeDates.Contains(checkDate))
            {
                checkDate = todayUtc.AddDays(-1);
            }
            while (activeDates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }

            dbUser.Streak = currentStreak;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { migratedCount, streak = dbUser.Streak });
        });

        // ==========================================
        // TOEIC VOCABULARY & PROGRESS ENDPOINTS
        // ==========================================

        app.MapGet("/api/user/game-progress", [Microsoft.AspNetCore.Authorization.Authorize] async (string? gameType, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var query = dbContext.UserGameProgresses.AsNoTracking().Where(g => g.UserId == userId);
            if (!string.IsNullOrWhiteSpace(gameType))
            {
                query = query.Where(g => g.GameType == gameType);
            }

            var list = await query.ToListAsync(cancellationToken);
            return Results.Ok(list.Select(g => new Backend.Application.DTOs.UserGameProgressDto(
                g.GameType,
                g.Level,
                g.CurrentStage,
                g.MaxUnlockedStage,
                g.HighScore,
                g.UpdatedAt
            )));
        });


        app.MapPost("/api/user/game-progress", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.SaveGameProgressRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            var existing = await dbContext.UserGameProgresses
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameType == request.GameType && g.Level == request.Level, cancellationToken);

            if (existing == null)
            {
                existing = new Backend.Domain.Entities.UserGameProgress
                {
                    UserId = userId,
                    GameType = request.GameType,
                    Level = request.Level,
                    CurrentStage = request.CurrentStage ?? 0,
                    MaxUnlockedStage = request.MaxUnlockedStage ?? 0,
                    HighScore = request.Score ?? 0,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.UserGameProgresses.Add(existing);
            }
            else
            {
                if (request.CurrentStage.HasValue) existing.CurrentStage = request.CurrentStage.Value;
                if (request.MaxUnlockedStage.HasValue && request.MaxUnlockedStage.Value > existing.MaxUnlockedStage)
                    existing.MaxUnlockedStage = request.MaxUnlockedStage.Value;
                if (request.Score.HasValue && request.Score.Value > existing.HighScore)
                    existing.HighScore = request.Score.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new Backend.Application.DTOs.UserGameProgressDto(
                existing.GameType,
                existing.Level,
                existing.CurrentStage,
                existing.MaxUnlockedStage,
                existing.HighScore,
                existing.UpdatedAt
            ));
        });


        app.MapPost("/api/user/game-progress/migrate", [Microsoft.AspNetCore.Authorization.Authorize] async (Backend.Application.DTOs.MigrateGameProgressRequest request, System.Security.Claims.ClaimsPrincipal user, Backend.Infrastructure.Persistence.AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var userIdString = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Results.Unauthorized();

            if (request.ProgressItems != null)
            {
                foreach (var item in request.ProgressItems)
                {
                    var existing = await dbContext.UserGameProgresses
                        .FirstOrDefaultAsync(g => g.UserId == userId && g.GameType == item.GameType && g.Level == item.Level, cancellationToken);
                    if (existing == null)
                    {
                        dbContext.UserGameProgresses.Add(new Backend.Domain.Entities.UserGameProgress
                        {
                            UserId = userId,
                            GameType = item.GameType,
                            Level = item.Level,
                            CurrentStage = item.CurrentStage ?? 0,
                            MaxUnlockedStage = item.MaxUnlockedStage ?? 0,
                            HighScore = item.Score ?? 0,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        if (item.CurrentStage.HasValue) existing.CurrentStage = item.CurrentStage.Value;
                        if (item.MaxUnlockedStage.HasValue && item.MaxUnlockedStage.Value > existing.MaxUnlockedStage)
                            existing.MaxUnlockedStage = item.MaxUnlockedStage.Value;
                        if (item.Score.HasValue && item.Score.Value > existing.HighScore)
                            existing.HighScore = item.Score.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(new { success = true, count = request.ProgressItems?.Count ?? 0 });
        });

        // ==========================================
        // EXAM CHECKPOINTS ENDPOINTS
        // ==========================================

        return app;
    }
}
