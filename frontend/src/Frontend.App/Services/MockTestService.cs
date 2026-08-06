using System.Net.Http.Json;
using System.Net.Http.Headers;
using Frontend.App.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.App.Services;

public class MockTestService
{
    private readonly HttpClient _http;

    public MockTestService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MockTestDto>> GetMockTestsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MockTestDto>>("api/mock-tests") ?? new List<MockTestDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching mock tests: {ex.Message}");
            return new List<MockTestDto>();
        }
    }

    public async Task<bool> CreateMockTestAsync(CreateMockTestRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mock-tests", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateMockTestAsync(int id, CreateMockTestRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mock-tests/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> UploadFileAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        
        // Cấu hình stream upload (tối đa 10MB)
        using var stream = file.OpenReadStream(10485760); 
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        
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
