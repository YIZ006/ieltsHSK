using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;

namespace Frontend.App.Services;

public sealed class ExamSessionService(IJSRuntime js)
{
    public Task<ExamSessionSnapshot> StartAsync(string storageKey, int durationSeconds)
        => js.InvokeAsync<ExamSessionSnapshot>("ExamSession.start", storageKey, durationSeconds).AsTask();

    public Task<ExamSessionSnapshot> GetAsync(string storageKey)
        => js.InvokeAsync<ExamSessionSnapshot>("ExamSession.get", storageKey).AsTask();

    public Task CompleteAsync(string storageKey)
        => js.InvokeVoidAsync("ExamSession.complete", storageKey, false).AsTask();

    public Task CompleteAsync(string storageKey, bool timedOut)
        => js.InvokeVoidAsync("ExamSession.complete", storageKey, timedOut).AsTask();

    public Task SetUnloadWarningAsync(bool enabled)
        => js.InvokeVoidAsync("ExamSession.setUnloadWarning", enabled).AsTask();

    public Task<bool> ConfirmLeaveAsync()
        => js.InvokeAsync<bool>("ExamSession.confirmLeave").AsTask();

    public Task LockTestContentAsync(string selector)
        => js.InvokeVoidAsync("ExamSession.lockTestContent", selector).AsTask();

    // ── LƯU / ĐỌC / XOÁ TIẾN TRÌNH BÀI THI (localStorage) ──
    public Task SaveProgressAsync(string storageKey, object state)
        => js.InvokeVoidAsync("ExamSession.saveProgress", storageKey, state).AsTask();

    public async Task<T?> LoadProgressAsync<T>(string storageKey)
        => await js.InvokeAsync<T?>("ExamSession.loadProgress", storageKey);

    public Task ClearProgressAsync(string storageKey)
        => js.InvokeVoidAsync("ExamSession.clearProgress", storageKey).AsTask();

    public Task ClearAsync(string storageKey)
        => js.InvokeVoidAsync("ExamSession.clearProgress", storageKey).AsTask();

    public static string CreateStorageKey(string skill, string? sessionId, string examUrl)
    {
        var identity = string.IsNullOrWhiteSpace(sessionId) ? examUrl : sessionId;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{skill}:{identity}"));
        return $"ielts-exam-session:{skill}:{Convert.ToHexString(bytes)}";
    }
}

public sealed class ExamSessionSnapshot
{
    public int SecondsRemaining { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsExpired { get; set; }
    public bool IsTimedOut { get; set; }
}
