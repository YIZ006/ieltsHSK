using System.Net.Http.Json;
using System.Text.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class StoryService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public StoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<StoryListItemModel>> GetStoriesAsync(string? level = null, string? category = null, string? search = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(level)) query.Add($"level={Uri.EscapeDataString(level)}");
            if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");

            var url = "api/stories" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var response = await _httpClient.GetFromJsonAsync<List<StoryListItemModel>>(url);
            return response ?? new List<StoryListItemModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading stories: {ex.Message}");
            return new List<StoryListItemModel>();
        }
    }

    public async Task<StoryModel?> GetStoryAsync(string idOrSlug)
    {
        try
        {
            var story = await _httpClient.GetFromJsonAsync<StoryModel>($"api/stories/{idOrSlug}", JsonOptions);
            if (story != null)
            {
                ParseStoryJson(story);
            }
            return story;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading story {idOrSlug}: {ex.Message}");
            return null;
        }
    }

    public async Task<StoryQuizResultModel?> SubmitQuizAsync(int storyId, List<int> answers)
    {
        try
        {
            var req = new { StoryId = storyId, Answers = answers };
            var response = await _httpClient.PostAsJsonAsync($"api/stories/{storyId}/quiz-submit", req);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StoryQuizResultModel>(JsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting quiz: {ex.Message}");
        }
        return null;
    }

    // Admin methods
    public async Task<List<StoryModel>> AdminGetAllStoriesAsync()
    {
        try
        {
            var list = await _httpClient.GetFromJsonAsync<List<StoryModel>>("api/admin/stories", JsonOptions);
            if (list != null)
            {
                foreach (var item in list)
                {
                    ParseStoryJson(item);
                }
                return list;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting admin stories: {ex.Message}");
        }
        return new List<StoryModel>();
    }

    public async Task<(bool Success, string? Url, string? JsonContent, string Message)> UploadStoryJsonAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(10 * 1024 * 1024);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("api/admin/stories/upload-json", content);
            if (response.IsSuccessStatusCode)
            {
                var dict = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(JsonOptions);
                var url = dict?.GetValueOrDefault("url")?.ToString() ?? dict?.GetValueOrDefault("Url")?.ToString();
                var jsonContent = dict?.GetValueOrDefault("jsonContent")?.ToString() ?? dict?.GetValueOrDefault("JsonContent")?.ToString();
                var msg = dict?.GetValueOrDefault("message")?.ToString() ?? dict?.GetValueOrDefault("Message")?.ToString() ?? "Tải file lên Cloudflare R2 thành công!";
                return (true, url, jsonContent, msg);
            }
            var err = await response.Content.ReadAsStringAsync();
            return (false, null, null, err);
        }
        catch (Exception ex)
        {
            return (false, null, null, "Lỗi upload: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> AdminCreateStoryAsync(StoryModel story)
    {
        try
        {
            var req = new
            {
                Title = story.Title,
                Slug = story.Slug,
                Level = story.Level,
                IeltsBand = story.IeltsBand,
                Category = story.Category,
                Summary = story.Summary,
                ThumbnailUrl = story.ThumbnailUrl,
                AudioUrl = story.AudioUrl,
                JsonUrl = story.JsonUrl,
                WordCount = story.WordCount,
                EstimatedMinutes = story.EstimatedMinutes,
                ContentJson = story.ContentJson,
                VocabularyJson = story.VocabularyJson,
                QuestionsJson = story.QuestionsJson,
                IsPublished = story.IsPublished
            };

            var response = await _httpClient.PostAsJsonAsync("api/admin/stories", req);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Tạo truyện thành công!");
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, "Lỗi mạng hoặc kết nối server: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> AdminUpdateStoryAsync(int id, StoryModel story)
    {
        try
        {
            var req = new
            {
                Title = story.Title,
                Slug = story.Slug,
                Level = story.Level,
                IeltsBand = story.IeltsBand,
                Category = story.Category,
                Summary = story.Summary,
                ThumbnailUrl = story.ThumbnailUrl,
                AudioUrl = story.AudioUrl,
                JsonUrl = story.JsonUrl,
                WordCount = story.WordCount,
                EstimatedMinutes = story.EstimatedMinutes,
                ContentJson = story.ContentJson,
                VocabularyJson = story.VocabularyJson,
                QuestionsJson = story.QuestionsJson,
                IsPublished = story.IsPublished
            };

            var response = await _httpClient.PutAsJsonAsync($"api/admin/stories/{id}", req);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Cập nhật truyện thành công!");
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, "Lỗi kết nối: " + ex.Message);
        }
    }

    public async Task<bool> AdminDeleteStoryAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/stories/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting story: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Success, string Message)> AdminImportJsonAsync(string jsonContent)
    {
        try
        {
            var req = new { JsonContent = jsonContent };
            var response = await _httpClient.PostAsJsonAsync("api/admin/stories/import-json", req);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Import truyện từ JSON thành công!");
            }
            var err = await response.Content.ReadAsStringAsync();
            return (false, err);
        }
        catch (Exception ex)
        {
            return (false, "Lỗi khi import JSON: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> SyncAllStoriesToR2Async()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/admin/stories/sync-to-r2", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(JsonOptions);
                var msg = result?.GetValueOrDefault("message")?.ToString() ?? result?.GetValueOrDefault("Message")?.ToString() ?? "Đồng bộ R2 thành công!";
                return (true, msg);
            }
            var err = await response.Content.ReadAsStringAsync();
            return (false, err);
        }
        catch (Exception ex)
        {
            return (false, "Lỗi khi đồng bộ R2: " + ex.Message);
        }
    }

    public async Task<string> AdminGetTemplateJsonAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("api/admin/stories/template-json");
            return response;
        }
        catch
        {
            return "{}";
        }
    }

    private static void ParseStoryJson(StoryModel story)
    {
        try
        {
            if (!string.IsNullOrEmpty(story.ContentJson))
            {
                story.Paragraphs = JsonSerializer.Deserialize<List<StoryParagraph>>(story.ContentJson, JsonOptions) ?? new();
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(story.VocabularyJson))
            {
                story.Vocabulary = JsonSerializer.Deserialize<List<StoryVocabulary>>(story.VocabularyJson, JsonOptions) ?? new();
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(story.QuestionsJson))
            {
                story.Questions = JsonSerializer.Deserialize<List<StoryQuestion>>(story.QuestionsJson, JsonOptions) ?? new();
            }
        }
        catch { }
    }
}
