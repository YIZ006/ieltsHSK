using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Api.Endpoints;

public static class ExamScheduleEndpoints
{
    public static IEndpointRouteBuilder MapExamScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── 1. Public Read Endpoints ───

        app.MapGet("/api/exam-schedules", async (
            string? certificateType,
            string? city,
            string? format,
            string? search,
            AppDbContext dbContext,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            var cacheKey = $"exam:schedules:list:{certificateType?.ToLowerInvariant()}:{city?.ToLowerInvariant()}:{format?.ToLowerInvariant()}:{search?.ToLowerInvariant()}";
            var cached = await cacheService.GetAsync<List<ExamScheduleDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var query = dbContext.ExamSchedules.Where(e => e.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(certificateType) && certificateType != "All" && certificateType != "Tất cả")
            {
                var certNorm = certificateType.Trim().ToLowerInvariant();
                query = query.Where(e => e.CertificateType.ToLower() == certNorm);
            }

            if (!string.IsNullOrWhiteSpace(city) && city != "All" && city != "Tất cả")
            {
                var cityNorm = city.Trim().ToLowerInvariant();
                query = query.Where(e => e.City.ToLower() == cityNorm);
            }

            if (!string.IsNullOrWhiteSpace(format) && format != "All" && format != "Tất cả")
            {
                var formatNorm = format.Trim().ToLowerInvariant();
                query = query.Where(e => e.Format.ToLower() == formatNorm);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.Location.ToLower().Contains(s) ||
                    e.Organizer.ToLower().Contains(s) ||
                    (e.Level != null && e.Level.ToLower().Contains(s)) ||
                    (e.Description != null && e.Description.ToLower().Contains(s)));
            }

            var items = await query
                .OrderBy(e => e.ExamDate)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(e => new ExamScheduleDto
            {
                Id = e.Id,
                CertificateType = e.CertificateType,
                Level = e.Level,
                ExamDate = e.ExamDate,
                RegistrationDeadline = e.RegistrationDeadline,
                Location = e.Location,
                City = e.City,
                Format = e.Format,
                FeeVnd = e.FeeVnd,
                Organizer = e.Organizer,
                Description = e.Description,
                RegistrationUrl = e.RegistrationUrl,
                Status = e.Status,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1), cancellationToken);
            return Results.Ok(dtos);
        });

        app.MapGet("/api/exam-schedules/upcoming", async (
            AppDbContext dbContext,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            const string cacheKey = "exam:schedules:upcoming";
            var cached = await cacheService.GetAsync<List<ExamScheduleDto>>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var todayUtc = DateTime.UtcNow.Date;
            var items = await dbContext.ExamSchedules
                .Where(e => e.IsActive && e.ExamDate >= todayUtc)
                .OrderBy(e => e.ExamDate)
                .Take(20)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(e => new ExamScheduleDto
            {
                Id = e.Id,
                CertificateType = e.CertificateType,
                Level = e.Level,
                ExamDate = e.ExamDate,
                RegistrationDeadline = e.RegistrationDeadline,
                Location = e.Location,
                City = e.City,
                Format = e.Format,
                FeeVnd = e.FeeVnd,
                Organizer = e.Organizer,
                Description = e.Description,
                RegistrationUrl = e.RegistrationUrl,
                Status = e.Status,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(30), cancellationToken);
            return Results.Ok(dtos);
        });

        app.MapGet("/api/exam-schedules/filters", async (
            AppDbContext dbContext,
            ICacheService cacheService,
            CancellationToken cancellationToken) =>
        {
            const string cacheKey = "exam:schedules:filters";
            var cached = await cacheService.GetAsync<ExamScheduleFiltersDto>(cacheKey, cancellationToken);
            if (cached != null) return Results.Ok(cached);

            var certTypes = await dbContext.ExamSchedules
                .Where(e => e.IsActive)
                .Select(e => e.CertificateType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

            var cities = await dbContext.ExamSchedules
                .Where(e => e.IsActive)
                .Select(e => e.City)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

            var formats = await dbContext.ExamSchedules
                .Where(e => e.IsActive)
                .Select(e => e.Format)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

            var organizers = await dbContext.ExamSchedules
                .Where(e => e.IsActive)
                .Select(e => e.Organizer)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

            var filters = new ExamScheduleFiltersDto
            {
                CertificateTypes = certTypes,
                Cities = cities,
                Formats = formats,
                Organizers = organizers
            };

            await cacheService.SetAsync(cacheKey, filters, TimeSpan.FromHours(2), cancellationToken);
            return Results.Ok(filters);
        });

        app.MapGet("/api/exam-schedules/{id:int}", async (
            int id,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var schedule = await dbContext.ExamSchedules.FindAsync(new object[] { id }, cancellationToken);
            if (schedule == null) return Results.NotFound(new { Message = "Không tìm thấy lịch thi." });

            var dto = new ExamScheduleDto
            {
                Id = schedule.Id,
                CertificateType = schedule.CertificateType,
                Level = schedule.Level,
                ExamDate = schedule.ExamDate,
                RegistrationDeadline = schedule.RegistrationDeadline,
                Location = schedule.Location,
                City = schedule.City,
                Format = schedule.Format,
                FeeVnd = schedule.FeeVnd,
                Organizer = schedule.Organizer,
                Description = schedule.Description,
                RegistrationUrl = schedule.RegistrationUrl,
                Status = schedule.Status,
                IsActive = schedule.IsActive,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            };

            return Results.Ok(dto);
        });

        // ─── 2. Admin Mutating Endpoints ───

        app.MapPost("/api/admin/exam-schedules",
            [Authorize(Roles = "admin")] async (
                CreateExamScheduleRequest request,
                AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.CertificateType))
                    return Results.BadRequest(new { Message = "Loại chứng chỉ không được để trống." });

                var location = !string.IsNullOrWhiteSpace(request.Location)
                    ? request.Location.Trim()
                    : request.TestCenter?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(location))
                    return Results.BadRequest(new { Message = "Địa điểm thi không được để trống." });

                if (string.IsNullOrWhiteSpace(request.City))
                    return Results.BadRequest(new { Message = "Tỉnh/Thành phố không được để trống." });

                if (string.IsNullOrWhiteSpace(request.Organizer))
                    return Results.BadRequest(new { Message = "Đơn vị tổ chức không được để trống." });

                var examDate = request.ExamDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.ExamDate, DateTimeKind.Utc)
                    : request.ExamDate.ToUniversalTime();

                var regDeadline = request.RegistrationDeadline.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc)
                    : request.RegistrationDeadline.ToUniversalTime();

                var entity = new ExamSchedule
                {
                    CertificateType = request.CertificateType.Trim(),
                    Level = request.Level?.Trim(),
                    ExamDate = examDate,
                    RegistrationDeadline = regDeadline,
                    Location = location,
                    City = request.City.Trim(),
                    Format = string.IsNullOrWhiteSpace(request.Format) ? "Computer" : request.Format.Trim(),
                    FeeVnd = request.FeeVnd,
                    Organizer = request.Organizer.Trim(),
                    Description = request.Description?.Trim(),
                    RegistrationUrl = request.RegistrationUrl?.Trim(),
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "RegistrationOpen" : request.Status.Trim(),
                    IsActive = request.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.ExamSchedules.Add(entity);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("exam:schedules:", cancellationToken);

                return Results.Created($"/api/exam-schedules/{entity.Id}", new { Id = entity.Id, Message = "Tạo lịch thi thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPut("/api/admin/exam-schedules/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                UpdateExamScheduleRequest request,
                AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var entity = await dbContext.ExamSchedules.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null) return Results.NotFound(new { Message = "Không tìm thấy lịch thi để cập nhật." });

                var location = !string.IsNullOrWhiteSpace(request.Location)
                    ? request.Location.Trim()
                    : request.TestCenter?.Trim() ?? entity.Location;

                var examDate = request.ExamDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.ExamDate, DateTimeKind.Utc)
                    : request.ExamDate.ToUniversalTime();

                var regDeadline = request.RegistrationDeadline.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc)
                    : request.RegistrationDeadline.ToUniversalTime();

                entity.CertificateType = string.IsNullOrWhiteSpace(request.CertificateType) ? entity.CertificateType : request.CertificateType.Trim();
                entity.Level = request.Level?.Trim();
                entity.ExamDate = examDate;
                entity.RegistrationDeadline = regDeadline;
                entity.Location = location;
                entity.City = string.IsNullOrWhiteSpace(request.City) ? entity.City : request.City.Trim();
                entity.Format = string.IsNullOrWhiteSpace(request.Format) ? entity.Format : request.Format.Trim();
                entity.FeeVnd = request.FeeVnd;
                entity.Organizer = string.IsNullOrWhiteSpace(request.Organizer) ? entity.Organizer : request.Organizer.Trim();
                entity.Description = request.Description?.Trim();
                entity.RegistrationUrl = request.RegistrationUrl?.Trim();
                if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = request.Status.Trim();
                if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
                entity.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await cacheService.RemoveByPrefixAsync("exam:schedules:", cancellationToken);

                return Results.Ok(new { Id = entity.Id, Message = "Cập nhật lịch thi thành công!" });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapDelete("/api/admin/exam-schedules/{id:int}",
            [Authorize(Roles = "admin")] async (
                int id,
                AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                var entity = await dbContext.ExamSchedules.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null) return Results.NotFound(new { Message = "Không tìm thấy lịch thi để xóa." });

                dbContext.ExamSchedules.Remove(entity);
                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveByPrefixAsync("exam:schedules:", cancellationToken);
                return Results.Ok(new { Message = "Đã xóa lịch thi thành công." });
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        app.MapPost("/api/admin/exam-schedules/import-json",
            [Authorize(Roles = "admin")] async (
                ImportExamScheduleJsonRequest request,
                AppDbContext dbContext,
                ICacheService cacheService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.JsonContent))
                    return Results.BadRequest(new { Message = "Nội dung JSON không được để trống." });

                try
                {
                    using var doc = JsonDocument.Parse(request.JsonContent);
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        return Results.BadRequest(new { Message = "JSON phải là một mảng danh sách lịch thi." });

                    var importedList = new List<ExamSchedule>();
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var certType = elem.TryGetProperty("certificateType", out var ctp) ? ctp.GetString() ?? "HSK" : "HSK";
                        var level = elem.TryGetProperty("level", out var lp) ? lp.GetString() : null;
                        var examDateStr = elem.TryGetProperty("examDate", out var edp) ? edp.GetString() : null;
                        var regDeadlineStr = elem.TryGetProperty("registrationDeadline", out var rdp) ? rdp.GetString() : null;
                        var location = elem.TryGetProperty("location", out var locp) ? locp.GetString() : null;
                        if (string.IsNullOrEmpty(location) && elem.TryGetProperty("testCenter", out var tcp))
                        {
                            location = tcp.GetString();
                        }
                        var city = elem.TryGetProperty("city", out var cip) ? cip.GetString() ?? "Hà Nội" : "Hà Nội";
                        var format = elem.TryGetProperty("format", out var fmp) ? fmp.GetString() ?? "Computer" : "Computer";
                        var feeVnd = elem.TryGetProperty("feeVnd", out var fvp) ? fvp.GetDecimal() : 0m;
                        var organizer = elem.TryGetProperty("organizer", out var ogp) ? ogp.GetString() ?? "" : "";
                        var desc = elem.TryGetProperty("description", out var dp) ? dp.GetString() : null;
                        var regUrl = elem.TryGetProperty("registrationUrl", out var rup) ? rup.GetString() : null;
                        var status = elem.TryGetProperty("status", out var stp) ? stp.GetString() ?? "RegistrationOpen" : "RegistrationOpen";

                        DateTime examDate = DateTime.TryParse(examDateStr, out var ed)
                            ? DateTime.SpecifyKind(ed, DateTimeKind.Utc)
                            : DateTime.UtcNow.AddMonths(1);
                        DateTime regDeadline = DateTime.TryParse(regDeadlineStr, out var rd)
                            ? DateTime.SpecifyKind(rd, DateTimeKind.Utc)
                            : examDate.AddDays(-14);

                        importedList.Add(new ExamSchedule
                        {
                            CertificateType = certType,
                            Level = level,
                            ExamDate = examDate,
                            RegistrationDeadline = regDeadline,
                            Location = location ?? "Địa điểm thi toàn quốc",
                            City = city,
                            Format = format,
                            FeeVnd = feeVnd,
                            Organizer = organizer,
                            Description = desc,
                            RegistrationUrl = regUrl,
                            Status = status,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (importedList.Count > 0)
                    {
                        dbContext.ExamSchedules.AddRange(importedList);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await cacheService.RemoveByPrefixAsync("exam:schedules:", cancellationToken);
                    }

                    return Results.Ok(new { Imported = importedList.Count, Message = $"Đã nhập thành công {importedList.Count} lịch thi." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Message = "Lỗi xử lý JSON: " + ex.Message });
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("admin"))
            .RequireRateLimiting("user-mutation");

        return app;
    }
}
