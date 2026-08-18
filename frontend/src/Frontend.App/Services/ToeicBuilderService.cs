using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Frontend.App.Services;

public class ToeicBuilderService
{
    private readonly HttpClient _http;

    public ToeicBuilderService(HttpClient http) => _http = http;

    /// <summary>Upload a single media file (image or audio) to R2 via backend. Returns public URL.</summary>
    public async Task<string?> UploadMediaAsync(System.Net.Http.MultipartFormDataContent content)
    {
        try
        {
            var response = await _http.PostAsync("api/toeic/upload-media", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
                return result?.Url;
            }
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ToeicBuilderService] Upload failed: {err}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicBuilderService] UploadMedia error: {ex.Message}");
            return null;
        }
    }

    /// <summary>Serialize exam data to JSON, upload to R2, save URL to DB. Returns R2 URL.</summary>
    public async Task<string?> SaveExamAsync(string collectionName, string title, object examData, int? mockTestId = null)
    {
        try
        {
            var payload = new
            {
                CollectionName = collectionName,
                Title = title,
                MockTestId = mockTestId,
                ExamData = examData
            };

            var response = await _http.PostAsJsonAsync("api/toeic/save-exam", payload);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SaveExamResponse>();
                return result?.Url;
            }
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ToeicBuilderService] SaveExam failed: {err}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicBuilderService] SaveExam error: {ex.Message}");
            return null;
        }
    }

    private record UploadMediaResponse(string Url, string Type);
    private record SaveExamResponse(string Url);
}
