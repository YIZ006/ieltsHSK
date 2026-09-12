using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class ExamCheckpointService
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly HttpClient? _httpClient;

    public ExamCheckpointService(
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        HttpClient? httpClient = null)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _httpClient = httpClient;
    }

    public async Task<string> GetUserIdentifierAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value
                       ?? user.FindFirst(ClaimTypes.Email)?.Value;

                if (!string.IsNullOrWhiteSpace(sub))
                {
                    return $"user_{sub}";
                }
            }
        }
        catch
        {
            // Fallback
        }
        return "guest";
    }

    public static string BuildStorageKey(string userIdentifier, string skill, string examUrl, int? mockTestId = null)
    {
        var normUrl = ExamSubmissionService.NormalizeUrl(examUrl);
        var identity = mockTestId.HasValue ? $"id_{mockTestId.Value}:{normUrl}" : normUrl;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).Substring(0, 16);
        return $"exam_checkpoint:{userIdentifier}:{skill.ToLowerInvariant()}:{hash}";
    }

    public async Task SaveCheckpointAsync(ExamCheckpointDto checkpoint)
    {
        try
        {
            var userIdentifier = await GetUserIdentifierAsync();
            checkpoint.UserIdentifier = userIdentifier;
            checkpoint.LastSavedAt = DateTimeOffset.UtcNow;

            var key = BuildStorageKey(userIdentifier, checkpoint.Skill, checkpoint.ExamUrl, checkpoint.MockTestId);
            await _localStorage.SetItemAsync(key, checkpoint);

            if (_httpClient != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(checkpoint);
                    await _httpClient.PostAsJsonAsync("api/exam-checkpoints", new
                    {
                        UserIdentifier = userIdentifier,
                        Skill = checkpoint.Skill,
                        ExamUrl = checkpoint.ExamUrl,
                        MockTestId = checkpoint.MockTestId,
                        CheckpointDataJson = json,
                        SecondsRemaining = checkpoint.SecondsRemaining
                    });
                }
                catch
                {
                    // Non-fatal if offline
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamCheckpointService] Save failed: {ex.Message}");
        }
    }

    public async Task<ExamCheckpointDto?> GetCheckpointAsync(string skill, string examUrl, int? mockTestId = null)
    {
        try
        {
            var userIdentifier = await GetUserIdentifierAsync();
            var key = BuildStorageKey(userIdentifier, skill, examUrl, mockTestId);

            // 1. Check server first if httpClient is available
            if (_httpClient != null)
            {
                try
                {
                    var query = $"examUrl={Uri.EscapeDataString(examUrl)}&skill={Uri.EscapeDataString(skill)}&userIdentifier={Uri.EscapeDataString(userIdentifier)}";
                    var serverCp = await _httpClient.GetFromJsonAsync<ServerCheckpointDto>($"api/exam-checkpoints?{query}");
                    if (serverCp != null && !string.IsNullOrWhiteSpace(serverCp.CheckpointDataJson))
                    {
                        var cp = JsonSerializer.Deserialize<ExamCheckpointDto>(serverCp.CheckpointDataJson);
                        if (cp != null && (DateTimeOffset.UtcNow - cp.LastSavedAt).TotalHours < 48 && cp.SecondsRemaining > 5)
                        {
                            await _localStorage.SetItemAsync(key, cp);
                            return cp;
                        }
                    }
                }
                catch
                {
                    // Server check failed, fallback to local storage
                }
            }

            // 2. Fallback to local storage
            var checkpoint = await _localStorage.GetItemAsync<ExamCheckpointDto>(key);
            if (checkpoint != null && 
                checkpoint.UserIdentifier == userIdentifier &&
                (DateTimeOffset.UtcNow - checkpoint.LastSavedAt).TotalHours < 48 &&
                checkpoint.SecondsRemaining > 5)
            {
                return checkpoint;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamCheckpointService] Get failed: {ex.Message}");
        }
        return null;
    }

    public async Task ClearCheckpointAsync(string skill, string examUrl, int? mockTestId = null)
    {
        try
        {
            var userIdentifier = await GetUserIdentifierAsync();
            var key = BuildStorageKey(userIdentifier, skill, examUrl, mockTestId);
            await _localStorage.RemoveItemAsync(key);

            if (_httpClient != null)
            {
                try
                {
                    var query = $"examUrl={Uri.EscapeDataString(examUrl)}&skill={Uri.EscapeDataString(skill)}&userIdentifier={Uri.EscapeDataString(userIdentifier)}";
                    await _httpClient.DeleteAsync($"api/exam-checkpoints?{query}");
                }
                catch
                {
                    // Non-fatal if offline
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamCheckpointService] Clear failed: {ex.Message}");
        }
    }

    private sealed record ServerCheckpointDto(
        int Id,
        string UserIdentifier,
        string Skill,
        string ExamUrl,
        int? MockTestId,
        string CheckpointDataJson,
        int SecondsRemaining,
        DateTime LastSavedAt);
}
