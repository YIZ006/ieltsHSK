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
            return response ?? new List<LearningSectionDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching sections: {ex.Message}");
            return new List<LearningSectionDto>();
        }
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
}
