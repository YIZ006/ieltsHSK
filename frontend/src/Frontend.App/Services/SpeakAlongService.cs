using System.Net.Http.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class SpeakAlongService
{
    private readonly HttpClient _http;

    public SpeakAlongService(HttpClient http)
    {
        _http = http;
    }

    public async Task<SpeakAlongExamData?> LoadExamAsync(string part)
    {
        try
        {
            var url = $"sample-data/ielts-speak-along-{part}.json";
            var data = await _http.GetFromJsonAsync<SpeakAlongExamData>(url);
            return data;
        }
        catch
        {
            // Fallback sample data
            return GetSampleData(part);
        }
    }

    private SpeakAlongExamData GetSampleData(string part)
    {
        var items = part switch
        {
            "Part1" => new List<SpeakAlongItem>
            {
                new() { Id = 1, Question = "Do you like reading books?", ModelAnswer = "Yes, I love reading books, especially fiction. It helps me relax and learn new things." },
                new() { Id = 2, Question = "What kind of music do you enjoy?", ModelAnswer = "I enjoy pop and classical music. I find them very soothing and inspiring." }
            },
            "Part2" => new List<SpeakAlongItem>
            {
                new() { Id = 6, Question = "Describe a memorable trip you took.", ModelAnswer = "I once visited Paris. The Eiffel Tower was breathtaking. I enjoyed the food and the culture." },
                new() { Id = 7, Question = "Talk about a person who influenced you.", ModelAnswer = "My mother influenced me greatly. She taught me the value of hard work and kindness." }
            },
            _ => new List<SpeakAlongItem>
            {
                new() { Id = 11, Question = "Do you think technology has improved communication?", ModelAnswer = "Absolutely, technology has made communication faster and more accessible. However, it can also reduce face-to-face interaction." },
                new() { Id = 12, Question = "Should students be required to learn a second language?", ModelAnswer = "Yes, learning a second language opens up opportunities and broadens their perspective." }
            }
        };

        return new SpeakAlongExamData
        {
            Title = $"IELTS Speaking {part}",
            Level = "6.0-7.0",
            Part = part,
            Items = items
        };
    }
}