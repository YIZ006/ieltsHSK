using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Blazored.LocalStorage;
using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class ExamCheckpointService
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public ExamCheckpointService(ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
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
            var checkpoint = await _localStorage.GetItemAsync<ExamCheckpointDto>(key);

            // Kiểm tra tính hợp lệ: đúng tài khoản và chưa quá 48 tiếng
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamCheckpointService] Clear failed: {ex.Message}");
        }
    }
}
