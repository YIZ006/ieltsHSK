using System.Net.Http.Json;
using System.Net.Http.Headers;
using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.App.Services;

public class MockTestService
{
    private readonly HttpClient _http;
    private List<MockTestDto>? _cache;
    private List<MockTestDto>? _toeicCache;

    public MockTestService(HttpClient http)
    {
        _http = http;
    }

    public void InvalidateCache()
    {
        _cache = null;
        _toeicCache = null;
    }

    public async Task<List<MockTestDto>> GetToeicMockTestsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _toeicCache != null) return _toeicCache;

        try
        {
            var result = await _http.GetFromJsonAsync<List<MockTestDto>>("api/toeic/r2-tests");
            if (result != null && result.Count > 0)
            {
                _toeicCache = result;
                return _toeicCache;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MockTestService] Error fetching TOEIC R2 tests from API: {ex.Message}");
        }

        // Fallback: Check general mock tests if available
        try
        {
            var all = await GetMockTestsAsync(forceRefresh);
            var filtered = all.Where(t => !string.IsNullOrEmpty(t.ToeicUrl)
                && (t.CollectionName.Contains("TOEIC", StringComparison.OrdinalIgnoreCase)
                    || t.Title.Contains("TOEIC", StringComparison.OrdinalIgnoreCase)
                    || t.ToeicUrl.Contains("toeic", StringComparison.OrdinalIgnoreCase))).ToList();
            if (filtered.Count > 0)
            {
                _toeicCache = filtered;
                return _toeicCache;
            }
        }
        catch { }

        // Fallback: Direct known tests in Cuongkeng/Toeic Data
        _toeicCache = new List<MockTestDto>
        {
            new()
            {
                Id = 1008,
                CollectionName = "TOEIC ETS 2026",
                Title = "Test 8",
                ToeicUrl = "https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev/Cuongkeng/Toeic%20Data/TOEIC%20ETS%202026-Test%208.json"
            },
            new()
            {
                Id = 1009,
                CollectionName = "TOEIC ETS 2026",
                Title = "Test 9",
                ToeicUrl = "https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev/Cuongkeng/Toeic%20Data/TOEIC%20ETS%202026-Test%209.json"
            },
            new()
            {
                Id = 1010,
                CollectionName = "TOEIC ETS 2026",
                Title = "Test 10",
                ToeicUrl = "https://pub-91655bd1442d498b9788d1f8f8575587.r2.dev/Cuongkeng/Toeic%20Data/TOEIC%20ETS%202026-Test%2010.json"
            }
        };
        return _toeicCache;
    }

    public async Task<List<MockTestDto>> GetMockTestsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache != null) return _cache;

        try
        {
            var result = await _http.GetFromJsonAsync<List<MockTestDto>>("api/mock-tests");
            _cache = result ?? new List<MockTestDto>();
            EnsureDefaultMockTests(_cache);
            return _cache;
        }
        catch (HttpRequestException)
        {
            try
            {
                await Task.Delay(500);
                var result = await _http.GetFromJsonAsync<List<MockTestDto>>("api/mock-tests");
                _cache = result ?? new List<MockTestDto>();
                EnsureDefaultMockTests(_cache);
                return _cache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching mock tests on retry: {ex.Message}");
                _cache ??= new List<MockTestDto>();
                EnsureDefaultMockTests(_cache);
                return _cache;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching mock tests: {ex.Message}");
            _cache ??= new List<MockTestDto>();
            EnsureDefaultMockTests(_cache);
            return _cache;
        }
    }

    private static void EnsureDefaultMockTests(List<MockTestDto> tests)
    {
        // 1. Practise Test 1 (IELTS Mock Test 2025 December)
        if (!tests.Any(t => t.Title != null && t.Title.Contains("Practise Test 1", StringComparison.OrdinalIgnoreCase)))
        {
            tests.Add(new MockTestDto
            {
                Id = 1,
                CollectionName = "IELTS Mock Test 2025 December",
                Title = "Practise Test 1",
                ListeningUrl = "sample-data/listening-test-1.json",
                ReadingUrl = "sample-data/reading-test-v2.json",
                WritingUrl = "sample-data/writing-test-1.json",
                SpeakingUrl = "sample-data/speaking-test-1.json",
                ListeningAnswerUrl = "sample-data/IELTS_Mock_Test_2025_December_Listening_Practise_Test_1_IELTS_Online_Tests.answers.json",
                ReadingAnswerUrl = "sample-data/IELTS_Mock_Test_2025_December_Reading_Practise_Test_1_IELTS_Online_Tests.answers.json"
            });
        }

        // 2. Actual Test 1 (IELTS Recent Actual Tests Vol 1)
        if (!tests.Any(t => t.Title != null && t.Title.Contains("Actual Test 1", StringComparison.OrdinalIgnoreCase)))
        {
            tests.Add(new MockTestDto
            {
                Id = 101,
                CollectionName = "IELTS Recent Actual Tests Vol 1",
                Title = "Actual Test 1",
                ListeningUrl = "sample-data/listening-actual-vol1-test1.json",
                ListeningAnswerUrl = "sample-data/listening-actual-vol1-test1.answers.json"
            });
        }
    }

    public async Task<bool> CreateMockTestAsync(CreateMockTestRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mock-tests", request);
        if (response.IsSuccessStatusCode)
        {
            InvalidateCache();
            return true;
        }
        return false;
    }

    public async Task<bool> UpdateMockTestAsync(int id, CreateMockTestRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mock-tests/{id}", request);
        if (response.IsSuccessStatusCode)
        {
            InvalidateCache();
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteMockTestAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/mock-tests/{id}");
        if (response.IsSuccessStatusCode)
        {
            InvalidateCache();
            return true;
        }
        return false;
    }

    public async Task<string?> UploadFileAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        
        // Cấu hình stream upload (tối đa 100MB hỗ trợ cả file âm thanh MP3/WAV)
        using var stream = file.OpenReadStream(104857600); 
        var fileContent = new StreamContent(stream);
        var ct = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ct);
        
        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync("api/mock-tests/upload", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
            return result?.Url;
        }
        
        Console.WriteLine($"Upload failed: {await response.Content.ReadAsStringAsync()}");
        return null;
    }
}
