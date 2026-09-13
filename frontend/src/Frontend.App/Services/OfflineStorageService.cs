using System.Net.Http.Json;
using System.Text.Json;
using Frontend.App.Models;
using Microsoft.JSInterop;

namespace Frontend.App.Services;

public sealed class OfflineExamPackage
{
    public int MockTestId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public string? ListeningJson { get; set; }
    public string? ListeningAnswerJson { get; set; }
    public string? ListeningAudioUrl { get; set; }

    public string? ReadingJson { get; set; }
    public string? ReadingAnswerJson { get; set; }

    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class OfflineStorageService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private DotNetObjectReference<OfflineStorageService>? _dotNetRef;
    private bool _isInitialized = false;

    public event Action<bool>? NetworkStatusChanged;

    public OfflineStorageService(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("OfflineDB.registerNetworkStatus", _dotNetRef);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] InitializeAsync error: {ex.Message}");
        }
    }

    [JSInvokable]
    public void OnNetworkStatusChanged(bool isOnline)
    {
        try
        {
            NetworkStatusChanged?.Invoke(isOnline);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] NetworkStatusChanged invoke error: {ex.Message}");
        }
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("OfflineDB.isOnline");
        }
        catch
        {
            return true;
        }
    }

    public async Task<List<int>> GetDownloadedTestIdsAsync()
    {
        try
        {
            var keys = await _js.InvokeAsync<List<int>>("OfflineDB.getDownloadedTestIds");
            return keys ?? new List<int>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetDownloadedTestIds error: {ex.Message}");
            return new List<int>();
        }
    }

    public async Task<List<OfflineExamPackage>> GetAllOfflinePackagesAsync()
    {
        try
        {
            var list = await _js.InvokeAsync<List<OfflineExamPackage>>("OfflineDB.getAllExams");
            return list ?? new List<OfflineExamPackage>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetAllOfflinePackagesAsync error: {ex.Message}");
            return new List<OfflineExamPackage>();
        }
    }

    public async Task<bool> IsTestDownloadedAsync(int testId)
    {
        try
        {
            var data = await _js.InvokeAsync<object?>("OfflineDB.getExamData", testId);
            return data != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DownloadTestAsync(MockTestDto test, Action<string, int>? onProgress = null)
    {
        try
        {
            var package = new OfflineExamPackage
            {
                MockTestId = test.Id,
                CollectionName = test.CollectionName,
                Title = test.Title
            };

            onProgress?.Invoke("Đang tải dữ liệu đề thi...", 15);

            // 1. Tải Listening JSON & Answers
            if (!string.IsNullOrWhiteSpace(test.ListeningUrl))
            {
                try
                {
                    package.ListeningJson = await _http.GetStringAsync(test.ListeningUrl);
                    if (!string.IsNullOrWhiteSpace(package.ListeningJson))
                    {
                        // Trích xuất AudioUrl từ JSON nếu có
                        using var doc = JsonDocument.Parse(package.ListeningJson);
                        if (doc.RootElement.TryGetProperty("AudioUrl", out var audioProp) ||
                            doc.RootElement.TryGetProperty("audioUrl", out audioProp))
                        {
                            package.ListeningAudioUrl = audioProp.GetString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfflineStorageService] Error loading listening JSON: {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(test.ListeningAnswerUrl))
            {
                try
                {
                    package.ListeningAnswerJson = await _http.GetStringAsync(test.ListeningAnswerUrl);
                }
                catch { }
            }

            onProgress?.Invoke("Đang tải dữ liệu Reading...", 40);

            // 2. Tải Reading JSON & Answers
            if (!string.IsNullOrWhiteSpace(test.ReadingUrl))
            {
                try
                {
                    package.ReadingJson = await _http.GetStringAsync(test.ReadingUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfflineStorageService] Error loading reading JSON: {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(test.ReadingAnswerUrl))
            {
                try
                {
                    package.ReadingAnswerJson = await _http.GetStringAsync(test.ReadingAnswerUrl);
                }
                catch { }
            }

            // 3. Tải và Cache file âm thanh MP3
            var audioUrl = package.ListeningAudioUrl;
            if (!string.IsNullOrWhiteSpace(audioUrl))
            {
                onProgress?.Invoke("Đang tải file âm thanh bài nghe (MP3)...", 65);
                try
                {
                    await _js.InvokeVoidAsync("OfflineDB.cacheMedia", audioUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfflineStorageService] Error caching audio: {ex.Message}");
                }
            }

            onProgress?.Invoke("Đang hoàn tất lưu vào bộ nhớ máy...", 90);

            // 4. Lưu gói đề thi vào IndexedDB
            await _js.InvokeVoidAsync("OfflineDB.saveExamData", test.Id, package);

            onProgress?.Invoke("Đã tải xong!", 100);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] DownloadTestAsync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteTestDownloadAsync(int testId, string? audioUrl = null)
    {
        try
        {
            // Xóa file âm thanh khỏi Cache
            if (!string.IsNullOrWhiteSpace(audioUrl))
            {
                await _js.InvokeVoidAsync("OfflineDB.removeMedia", audioUrl);
            }
            else
            {
                // Thử đọc package để lấy audioUrl nếu chưa truyền vào
                var pkg = await GetOfflinePackageAsync(testId);
                if (!string.IsNullOrWhiteSpace(pkg?.ListeningAudioUrl))
                {
                    await _js.InvokeVoidAsync("OfflineDB.removeMedia", pkg.ListeningAudioUrl);
                }
            }

            // Xóa đề khỏi IndexedDB
            await _js.InvokeVoidAsync("OfflineDB.removeExamData", testId);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] DeleteTestDownloadAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<OfflineExamPackage?> GetOfflinePackageAsync(int testId)
    {
        try
        {
            return await _js.InvokeAsync<OfflineExamPackage?>("OfflineDB.getExamData", testId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetOfflinePackageAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ExamData?> GetOfflineExamModelAsync(int testId, string skill)
    {
        try
        {
            var pkg = await GetOfflinePackageAsync(testId);
            if (pkg == null) return null;

            string? rawJson = skill.ToLowerInvariant() switch
            {
                "listening" => pkg.ListeningJson,
                "reading" => pkg.ReadingJson,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            return JsonSerializer.Deserialize<ExamData>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetOfflineExamModelAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ExamAnswerKey?> GetOfflineAnswerKeyAsync(int testId, string skill)
    {
        try
        {
            var pkg = await GetOfflinePackageAsync(testId);
            if (pkg == null) return null;

            string? rawJson = skill.ToLowerInvariant() switch
            {
                "listening" => pkg.ListeningAnswerJson,
                "reading" => pkg.ReadingAnswerJson,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            return JsonSerializer.Deserialize<ExamAnswerKey>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetOfflineAnswerKeyAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> GetOfflineAudioBlobUrlAsync(string audioUrl)
    {
        try
        {
            return await _js.InvokeAsync<string?>("OfflineDB.getMediaBlobUrl", audioUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflineStorageService] GetOfflineAudioBlobUrlAsync error: {ex.Message}");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
    }
}
