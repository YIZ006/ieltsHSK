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

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ai/grade-writing", [Microsoft.AspNetCore.Authorization.Authorize] async (
            Backend.Application.DTOs.GradeWritingRequest request,
            Backend.Application.Abstractions.IAiGradingService aiService,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.GradeWritingAsync(request, cancellationToken);
            return Results.Ok(result);
        });


        app.MapPost("/api/ai/grade-speaking", [Microsoft.AspNetCore.Authorization.Authorize] async (
            Backend.Application.DTOs.GradeSpeakingRequest request,
            Backend.Application.Abstractions.IAiGradingService aiService,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.GradeSpeakingAsync(request, cancellationToken);
            return Results.Ok(result);
        });

        // ─── SPEAKING: Upload audio riêng tư lên R2 Private ───

        app.MapGet("/api/admin/ai/config", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] () =>
        {
            var settings = LoadAiSettings();
            return Results.Ok(settings);
        });


        app.MapPost("/api/admin/ai/config", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] (SystemAiSettingsDto dto) =>
        {
            SaveAiSettings(dto);
            return Results.Ok(new { success = true, message = "Đã lưu cấu hình API Keys thành công." });
        });


        app.MapPost("/api/admin/ai/test-connection", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (TestAiConnectionRequestDto req, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(req.ApiKey))
            {
                return Results.Ok(new { success = false, message = "Vui lòng nhập API Key trước khi kiểm tra.", latencyMs = 0 });
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            try
            {
                var provider = (req.Provider ?? "").ToLowerInvariant();

                if (provider == "gemini")
                {
                    var testUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={req.ApiKey.Trim()}";
                    var res = await http.GetAsync(testUrl, cancellationToken);
                    stopwatch.Stop();

                    if (res.IsSuccessStatusCode)
                    {
                        var models = new List<string>();
                        try
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("models", out var modelsArr) && modelsArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in modelsArr.EnumerateArray())
                                {
                                    if (m.TryGetProperty("name", out var nameElem))
                                    {
                                        var name = nameElem.GetString() ?? "";
                                        if (name.StartsWith("models/")) name = name.Substring(7);
                                        if (!string.IsNullOrWhiteSpace(name) && !models.Contains(name)) models.Add(name);
                                    }
                                }
                            }
                        }
                        catch { }

                        return Results.Ok(new {
                            success = true,
                            message = $"Kết nối Google Gemini thành công! (Tìm thấy {models.Count} models, độ trễ: {stopwatch.ElapsedMilliseconds}ms)",
                            latencyMs = stopwatch.ElapsedMilliseconds,
                            availableModels = models
                        });
                    }
                    else
                    {
                        var errContent = await res.Content.ReadAsStringAsync(cancellationToken);
                        return Results.Ok(new { success = false, message = $"Lỗi từ Google AI: {(int)res.StatusCode} {res.ReasonPhrase}", latencyMs = stopwatch.ElapsedMilliseconds, detail = errContent });
                    }
                }
                else if (provider == "xkiro" || provider == "openai" || provider == "deepseek" || provider == "whisper" || provider == "groq")
                {
                    var baseUrl = (req.BaseUrl ?? "").TrimEnd('/');
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        baseUrl = provider switch {
                            "xkiro" => "https://api.xkiro.com/v1",
                            "deepseek" => "https://api.deepseek.com/v1",
                            "groq" => "https://api.groq.com/openai/v1",
                            _ => "https://api.openai.com/v1"
                        };
                    }

                    var testUrl = $"{baseUrl}/models";
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.ApiKey.Trim());
            
                    var res = await http.GetAsync(testUrl, cancellationToken);
                    stopwatch.Stop();

                    if (res.IsSuccessStatusCode)
                    {
                        var models = new List<string>();
                        try
                        {
                            var json = await res.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in dataArr.EnumerateArray())
                                {
                                    if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("id", out var idElem))
                                    {
                                        var id = idElem.GetString();
                                        if (!string.IsNullOrWhiteSpace(id) && !models.Contains(id)) models.Add(id);
                                    }
                                    else if (m.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        var str = m.GetString();
                                        if (!string.IsNullOrWhiteSpace(str) && !models.Contains(str)) models.Add(str);
                                    }
                                }
                            }
                            else if (root.TryGetProperty("models", out var modelsArr) && modelsArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var m in modelsArr.EnumerateArray())
                                {
                                    if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("id", out var idElem))
                                    {
                                        var id = idElem.GetString();
                                        if (!string.IsNullOrWhiteSpace(id) && !models.Contains(id)) models.Add(id);
                                    }
                                    else if (m.ValueKind == System.Text.Json.JsonValueKind.Object && m.TryGetProperty("name", out var nameElem))
                                    {
                                        var name = nameElem.GetString();
                                        if (!string.IsNullOrWhiteSpace(name) && !models.Contains(name)) models.Add(name);
                                    }
                                }
                            }
                        }
                        catch { }

                        var provName = provider switch {
                            "whisper" => "OpenAI Whisper",
                            "groq" => "Groq Cloud Whisper",
                            _ => provider.ToUpper()
                        };
                        return Results.Ok(new {
                            success = true,
                            message = $"Kết nối {provName} thành công! (Tìm thấy {models.Count} models, độ trễ: {stopwatch.ElapsedMilliseconds}ms)",
                            latencyMs = stopwatch.ElapsedMilliseconds,
                            availableModels = models
                        });
                    }
                    else
                    {
                        var errContent = await res.Content.ReadAsStringAsync(cancellationToken);
                        return Results.Ok(new { success = false, message = $"Lỗi kết nối {(int)res.StatusCode}: {res.ReasonPhrase}", latencyMs = stopwatch.ElapsedMilliseconds, detail = errContent });
                    }
                }
                else
                {
                    return Results.Ok(new { success = false, message = $"Nhà cung cấp '{req.Provider}' không được hỗ trợ.", latencyMs = 0 });
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return Results.Ok(new { success = false, message = $"Không thể kết nối máy chủ: {ex.Message}", latencyMs = stopwatch.ElapsedMilliseconds });
            }
        });

        // --- ADMIN USER MANAGEMENT ENDPOINTS ---


        return app;
    }

    private static SystemAiSettingsDto LoadAiSettings()
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Backend.Api", "ai_settings.json")
        };

        foreach (var p in paths)
        {
            if (File.Exists(p))
            {
                try
                {
                    var json = File.ReadAllText(p);
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<SystemAiSettingsDto>(json);
                    if (parsed != null) return parsed;
                }
                catch { }
            }
        }
        return new SystemAiSettingsDto();
    }

    private static void SaveAiSettings(SystemAiSettingsDto settings)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var targetPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "ai_settings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "ai_settings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Backend.Api", "ai_settings.json")
            };

            foreach (var p in targetPaths)
            {
                try
                {
                    var dir = Path.GetDirectoryName(p);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        File.WriteAllText(p, json);
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiSettings] Failed to save settings: {ex.Message}");
        }
    }
}
