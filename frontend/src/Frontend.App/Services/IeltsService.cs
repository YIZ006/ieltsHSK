using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class IeltsService
{
    private readonly HttpClient _httpClient;

    public IeltsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // === Vocabulary (IELTS) ===
    public async Task<List<IeltsVocabularyItem>?> GetVocabularyAsync(string? topic = null, string? search = null)
    {
        try
        {
            var url = "api/ielts/vocab";
            var qs = new List<string>();
            if (!string.IsNullOrEmpty(topic)) qs.Add($"topic={Uri.EscapeDataString(topic)}");
            if (!string.IsNullOrEmpty(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
            if (qs.Count > 0) url += "?" + string.Join("&", qs);
            var items = await _httpClient.GetFromJsonAsync<List<IeltsVocabularyItem>>(url);
            return items ?? new List<IeltsVocabularyItem>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CreateVocabularyAsync(IeltsVocabularyItem item)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ielts/vocab", item);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateVocabularyAsync(int id, IeltsVocabularyItem item)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/ielts/vocab/{id}", item);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteVocabularyAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/ielts/vocab/{id}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<(bool Success, int Deleted)> DeleteAllVocabularyAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/ielts/vocab/all");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
                return (true, result?.GetValueOrDefault("Deleted") ?? 0);
            }
            return (false, 0);
        }
        catch { return (false, 0); }
    }

    public async Task<IeltsImportExcelResponse?> ImportVocabularyExcelAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file, string mode = "skip")
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(20 * 1024 * 1024));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(mode), "mode");

            var response = await _httpClient.PostAsync("api/ielts/vocab/import-excel", content);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IeltsImportExcelResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IeltsImportExcelResponse?> ImportMultipleVocabularyExcelAsync(IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files, string mode = "skip")
    {
        try
        {
            using var content = new MultipartFormDataContent();
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(20 * 1024 * 1024));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }
            content.Add(new StringContent(mode), "mode");

            var response = await _httpClient.PostAsync("api/ielts/vocab/import-multiple", content);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IeltsImportExcelResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CourseDto>> GetCoursesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<CourseDto>>("api/ielts/courses");
            return response ?? new List<CourseDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching courses: {ex.Message}");
            return new List<CourseDto>();
        }
    }

    public async Task<List<WebsiteDto>> GetWebsitesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<WebsiteDto>>("api/ielts/websites");
            return response ?? new List<WebsiteDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching websites: {ex.Message}");
            return new List<WebsiteDto>();
        }
    }

    public async Task<List<LearningSectionDto>> GetSectionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<LearningSectionDto>>("api/ielts/sections");
            if (response != null && response.Count > 0)
                return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching sections: {ex.Message}");
        }

        // Fallback sections if API returns empty or fails
        return new List<LearningSectionDto>
        {
            new LearningSectionDto { Name = "Luyện nghe", Route = "/ielts/listening", Icon = "bi-headphones", Description = "Luyện kỹ năng nghe với các bài tập đa dạng" },
            new LearningSectionDto { Name = "Luyện đọc", Route = "/ielts/reading", Icon = "bi-book", Description = "Đọc hiểu và phân tích văn bản" },
            new LearningSectionDto { Name = "Luyện viết", Route = "/ielts/writing", Icon = "bi-pencil", Description = "Thực hành viết luận và báo cáo" },
            new LearningSectionDto { Name = "Luyện nói", Route = "/ielts/speaking", Icon = "bi-mic", Description = "Luyện nói với các chủ đề thường gặp" },
            new LearningSectionDto { Name = "Nói theo", Route = "/ielts/speak-along", Icon = "bi-chat-dots", Description = "Shadowing - luyện phát âm và ngữ điệu" },
            new LearningSectionDto { Name = "Truyện song ngữ", Route = "/ielts/stories", Icon = "bi-journal-text", Description = "Đọc truyện song ngữ Anh-Việt" }
        };
    }

    public async Task<bool> UpdateUserLevelAsync(string level)
    {
        try
        {
            var request = new { Level = level };
            var response = await _httpClient.PutAsJsonAsync("api/user/level", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating level: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ListenVideoDto>> GetListenFillVideosAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ListenVideoDto>>("api/listen-videos");
            return response ?? new List<ListenVideoDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching listen videos: {ex.Message}");
            return new List<ListenVideoDto>();
        }
    }

    public async Task<ListenVideoDto?> GetListenFillVideoByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ListenVideoDto>($"api/listen-videos/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching video: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SubmitListenVideoAsync(string youtubeUrl)
    {
        try
        {
            var request = new ListenVideoSubmitRequest { YoutubeUrl = youtubeUrl };
            var response = await _httpClient.PostAsJsonAsync("api/listen-videos/submit", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting video: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ListenVideoDto>> GetAllListenVideosAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ListenVideoDto>>("api/admin/listen-videos");
            return response ?? new List<ListenVideoDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching all videos: {ex.Message}");
            return new List<ListenVideoDto>();
        }
    }

    public async Task<(bool Success, string Message)> ApproveListenVideoAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/admin/listen-videos/{id}/approve", null);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Phê duyệt thành công.");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                try 
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorText, options);
                    return (false, dict?.GetValueOrDefault("message") ?? dict?.GetValueOrDefault("Message") ?? errorText);
                }
                catch
                {
                    return (false, errorText);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error approving video: {ex.Message}");
            return (false, $"Lỗi mạng hoặc server: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateManualTranscriptAsync(int id, string transcriptText)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/listen-videos/{id}/transcript", new { TranscriptText = transcriptText });
            if (response.IsSuccessStatusCode)
            {
                return (true, "Cập nhật phụ đề thủ công thành công.");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                try 
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorText, options);
                    return (false, dict?.GetValueOrDefault("message") ?? dict?.GetValueOrDefault("Message") ?? errorText);
                }
                catch
                {
                    return (false, errorText);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating manual transcript: {ex.Message}");
            return (false, $"Lỗi mạng hoặc server: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateListenVideoDetailsAsync(int id, string title, string level, string category)
    {
        try
        {
            var req = new UpdateListenVideoRequest
            {
                Title = title,
                Level = level,
                Category = category
            };
            var response = await _httpClient.PutAsJsonAsync($"api/admin/listen-videos/{id}", req);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Cập nhật thông tin video thành công.");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorText, options);
                    return (false, dict?.GetValueOrDefault("message") ?? dict?.GetValueOrDefault("Message") ?? errorText);
                }
                catch
                {
                    return (false, errorText);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating video details: {ex.Message}");
            return (false, $"Lỗi mạng hoặc server: {ex.Message}");
        }
    }

    public async Task<bool> DeleteListenVideoAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/listen-videos/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting video: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Success, string Message)> ImportListenVideosExcelAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(10 * 1024 * 1024)); // Max 10MB
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("api/admin/listen-videos/import-excel", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return (true, result?.GetValueOrDefault("message")?.ToString() ?? "Import thành công");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                try 
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorText, options);
                    return (false, dict?.GetValueOrDefault("message") ?? dict?.GetValueOrDefault("Message") ?? errorText);
                }
                catch
                {
                    return (false, "Lỗi server: " + errorText);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing excel: {ex.Message}");
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }
}

// === DTOs: IELTS Vocabulary ===
public class IeltsVocabularyItem
{
    public int Id { get; set; }
    public string Word { get; set; } = "";
    public string? Phonetic { get; set; }
    public string? PartOfSpeech { get; set; }
    public string Meaning { get; set; } = "";
    public string? Example { get; set; }
    public string? ExampleMeaning { get; set; }
    public string? Topic { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public record IeltsImportExcelResponse(int Success, int Fail, int Duplicate, int Updated, string? JsonUrl, List<string>? Errors);
