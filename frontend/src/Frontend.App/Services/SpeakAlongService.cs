using System.Net.Http.Json;
using System.Text.Json;
using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Frontend.App.Services;

public class SpeakAlongService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly string _backendApiBaseUrl;

    public SpeakAlongService(HttpClient http, IJSRuntime js, string? backendApiBaseUrl = null)
    {
        _http = http;
        _js = js;
        _backendApiBaseUrl = !string.IsNullOrWhiteSpace(backendApiBaseUrl) 
            ? backendApiBaseUrl.TrimEnd('/') 
            : "http://localhost:5101";
    }

    private string GetStorageKey(string part)
    {
        var clean = part.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
        return $"custom_speak_along_{clean}";
    }

    public async Task<SpeakAlongExamData?> LoadExamAsync(string part)
    {
        var cleanPart = part.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");

        // 1. Try to fetch from Backend & Cloudflare R2
        try
        {
            var apiEndpoint = $"{_backendApiBaseUrl}/api/ielts/speak-along/{cleanPart}";
            using var backendClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await backendClient.GetAsync(apiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (doc.TryGetProperty("dataUrl", out var dataUrlProp))
                {
                    var r2Url = dataUrlProp.GetString();
                    if (!string.IsNullOrWhiteSpace(r2Url))
                    {
                        var r2Data = await backendClient.GetFromJsonAsync<SpeakAlongExamData>(r2Url);
                        if (r2Data != null && r2Data.Items != null && r2Data.Items.Count > 0)
                        {
                            // Cache to localStorage for offline
                            var key = GetStorageKey(part);
                            var json = JsonSerializer.Serialize(r2Data);
                            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
                            return r2Data;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Remote R2 load failed, falling back: {ex.Message}");
        }

        // 2. Try localStorage cache
        try
        {
            var key = GetStorageKey(part);
            var localJson = await _js.InvokeAsync<string?>("localStorage.getItem", key);
            if (!string.IsNullOrWhiteSpace(localJson))
            {
                var localData = JsonSerializer.Deserialize<SpeakAlongExamData>(localJson);
                if (localData != null && localData.Items != null && localData.Items.Count > 0)
                {
                    return localData;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Error reading local storage: {ex.Message}");
        }

        // 3. Fallback to sample data files
        try
        {
            string url;
            if (cleanPart.Contains("100") || cleanPart.Contains("all") || cleanPart.Contains("full") || cleanPart.Contains("essential"))
            {
                url = "sample-data/ielts-speak-along-100-sentences.json";
            }
            else
            {
                if (!cleanPart.StartsWith("part")) cleanPart = "part" + cleanPart;
                url = $"sample-data/ielts-speak-along-{cleanPart}.json";
            }

            var data = await _http.GetFromJsonAsync<SpeakAlongExamData>(url);
            if (data != null && data.Items != null && data.Items.Count > 0)
            {
                return data;
            }
            return GetSampleData(part);
        }
        catch
        {
            return GetSampleData(part);
        }
    }

    public async Task<string?> SaveExamDataAsync(string part, SpeakAlongExamData data)
    {
        string? r2Url = null;

        // 1. Upload & Save to Cloudflare R2 via Backend API
        try
        {
            var apiEndpoint = $"{_backendApiBaseUrl}/api/ielts/speak-along/save";
            using var backendClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            
            var payload = new
            {
                Part = part,
                Data = data
            };

            var response = await backendClient.PostAsJsonAsync(apiEndpoint, payload);
            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (resJson.TryGetProperty("r2Url", out var r2Prop))
                {
                    r2Url = r2Prop.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Backend R2 save warning: {ex.Message}");
        }

        // 2. Also cache in localStorage
        try
        {
            var key = GetStorageKey(part);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] localStorage cache error: {ex.Message}");
        }

        return r2Url;
    }

    public async Task<bool> UploadJsonFileToR2Async(IBrowserFile file, string part)
    {
        try
        {
            var apiEndpoint = $"{_backendApiBaseUrl}/api/ielts/speak-along/upload-file";
            using var backendClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(part), "part");

            var response = await backendClient.PostAsync(apiEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                // Reload exam into local state
                await LoadExamAsync(part);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Upload file to R2 error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ImportJsonAsync(string jsonContent, string part)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            SpeakAlongExamData? data = null;
            try
            {
                data = JsonSerializer.Deserialize<SpeakAlongExamData>(jsonContent, options);
            }
            catch { }

            if (data == null || data.Items == null || data.Items.Count == 0)
            {
                var list = JsonSerializer.Deserialize<List<SpeakAlongItem>>(jsonContent, options);
                if (list != null && list.Count > 0)
                {
                    data = new SpeakAlongExamData
                    {
                        Title = $"IELTS Speaking Nói Theo - {part}",
                        Level = "IELTS 6.5 - 8.0+",
                        Part = part,
                        Items = list
                    };
                }
            }

            if (data != null && data.Items != null && data.Items.Count > 0)
            {
                for (int i = 0; i < data.Items.Count; i++)
                {
                    if (data.Items[i].Id <= 0)
                    {
                        data.Items[i].Id = i + 1;
                    }
                }
                await SaveExamDataAsync(part, data);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing JSON: {ex.Message}");
            return false;
        }
    }

    public async Task ResetToDefaultAsync(string part)
    {
        try
        {
            var key = GetStorageKey(part);
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting SpeakAlong data: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════
    // AUDIO SHADOWING (CAMBRIDGE IELTS & ENGNOVATE MP3)
    // ══════════════════════════════════════════════════════

    private const string AudioLessonsStorageKey = "ielts_audio_shadowing_lessons_catalog";

    public async Task<List<AudioLessonDto>> LoadAudioLessonsAsync()
    {
        // 1. Try to fetch master catalog from Backend & Cloudflare R2
        try
        {
            var apiEndpoint = $"{_backendApiBaseUrl}/api/ielts/audio-shadowing";
            using var backendClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await backendClient.GetAsync(apiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (doc.TryGetProperty("dataUrl", out var dataUrlProp))
                {
                    var r2Url = dataUrlProp.GetString();
                    if (!string.IsNullOrWhiteSpace(r2Url))
                    {
                        var r2Lessons = await backendClient.GetFromJsonAsync<List<AudioLessonDto>>(r2Url);
                        if (r2Lessons != null && r2Lessons.Count > 0)
                        {
                            // Cache to localStorage
                            var json = JsonSerializer.Serialize(r2Lessons);
                            await _js.InvokeVoidAsync("localStorage.setItem", AudioLessonsStorageKey, json);
                            return r2Lessons;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] R2 audio catalog fetch warning: {ex.Message}");
        }

        // 2. Try local storage cache
        try
        {
            var localJson = await _js.InvokeAsync<string?>("localStorage.getItem", AudioLessonsStorageKey);
            if (!string.IsNullOrWhiteSpace(localJson))
            {
                var cached = JsonSerializer.Deserialize<List<AudioLessonDto>>(localJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cached != null && cached.Count > 0)
                {
                    return cached;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Local storage audio lessons read failed: {ex.Message}");
        }

        // 3. Fallback to bundled master catalog json
        try
        {
            var masterCatalog = await _http.GetFromJsonAsync<List<AudioLessonDto>>("sample-data/ielts_audio_shadowing_master_catalog.json");
            if (masterCatalog != null && masterCatalog.Count > 0)
            {
                var json = JsonSerializer.Serialize(masterCatalog);
                await _js.InvokeVoidAsync("localStorage.setItem", AudioLessonsStorageKey, json);
                return masterCatalog;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Master catalog json load error: {ex.Message}");
        }

        return new List<AudioLessonDto>();
    }

    public async Task SaveAudioLessonsAsync(List<AudioLessonDto> lessons)
    {
        // 1. Sync to Cloudflare R2 via Backend API
        try
        {
            var apiEndpoint = $"{_backendApiBaseUrl}/api/ielts/audio-shadowing/save";
            using var backendClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            await backendClient.PostAsJsonAsync(apiEndpoint, lessons);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] R2 audio catalog save warning: {ex.Message}");
        }

        // 2. Cache in localStorage
        try
        {
            var json = JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
            await _js.InvokeVoidAsync("localStorage.setItem", AudioLessonsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpeakAlongService] Error saving audio lessons locally: {ex.Message}");
        }
    }

    public async Task SaveAudioLessonAsync(AudioLessonDto lesson)
    {
        var all = await LoadAudioLessonsAsync();
        var idx = all.FindIndex(l => l.Id == lesson.Id);
        if (idx >= 0)
        {
            all[idx] = lesson;
        }
        else
        {
            all.Insert(0, lesson);
        }
        await SaveAudioLessonsAsync(all);
    }

    public async Task DeleteAudioLessonAsync(string id)
    {
        var all = await LoadAudioLessonsAsync();
        all.RemoveAll(l => l.Id == id);
        await SaveAudioLessonsAsync(all);
    }

    private SpeakAlongExamData GetSampleData(string part)
    {
        return new SpeakAlongExamData
        {
            Title = $"IELTS Speaking Shadowing - {part}",
            Level = "IELTS 6.5 - 8.0+",
            Part = part,
            Items = new List<SpeakAlongItem>
            {
                new SpeakAlongItem
                {
                    Id = 1,
                    Question = "Where are you from?",
                    ModelAnswer = "I'm from Hanoi, the bustling capital of Vietnam. It's a vibrant city with a rich cultural heritage spanning over a thousand years.",
                    Vocabulary = "bustling capital (thủ đô nhộn nhịp), vibrant city (thành phố sôi động), cultural heritage (di sản văn hóa)",
                    Tips = "Nhấn mạnh các tính từ miêu tả 'bustling', 'vibrant'. Giữ nhịp nối âm mượt mà giữa 'from Hanoi' và 'rich cultural'."
                },
                new SpeakAlongItem
                {
                    Id = 2,
                    Question = "Do you prefer living in a house or an apartment?",
                    ModelAnswer = "I definitely prefer living in an apartment because of the modern amenities and 24/7 security. It also offers breathtaking panoramic views of the city skyline.",
                    Vocabulary = "modern amenities (tiện ích hiện đại), 24/7 security (an ninh 24/7), panoramic views (tầm nhìn toàn cảnh)",
                    Tips = "Lên giọng nhẹ ở 'apartment', sau đó hạ giọng dứt khoát ở cuối câu 'city skyline'."
                }
            }
        };
    }
}