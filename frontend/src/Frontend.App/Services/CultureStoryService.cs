using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.App.Services;

public class StorySentence
{
    public int Index { get; set; }
    public string Zh { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
}

public class StoryVocabItem
{
    public string Word { get; set; } = string.Empty;
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string SinoVi { get; set; } = string.Empty;
    public string? Pos { get; set; }
}

public class StoryQuizQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class CultureStoryItem
{
    public string Id { get; set; } = string.Empty;
    public int StoryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleZh { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Category { get; set; } = "Thành ngữ điển tích";
    public string Level { get; set; } = "Intermediate";
    public string? HskLevel { get; set; } = "HSK 3 - 4";
    public string? SummaryVi { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string ContentZh { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public List<StorySentence> Sentences { get; set; } = new();
    public List<StorySentence> Paragraphs { get; set; } = new();
    public List<StoryVocabItem> Vocabulary { get; set; } = new();
    public List<StoryQuizQuestion> Questions { get; set; } = new();
    public int EstimatedMinutes { get; set; } = 3;
    public int ViewsCount { get; set; }
}

public class CultureQuizResult
{
    public int StoryId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public double ScorePercentage { get; set; }
    public List<bool> AnswerCorrectness { get; set; } = new();
    public List<int> CorrectIndices { get; set; } = new();
    public List<string> Explanations { get; set; } = new();
}

public class CultureStoryService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private List<CultureStoryItem>? _cachedStories;

    public CultureStoryService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    private string GetApiUrl(string path)
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<List<CultureStoryItem>> GetStoriesAsync(
        string? category = null,
        string? level = null,
        string? search = null,
        bool forceRefresh = false)
    {
        // 1. Try Live Backend API
        try
        {
            var queryParams = new List<string>();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            queryParams.Add($"_t={ts}");

            if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "Tất cả")
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrWhiteSpace(level) && level != "All" && level != "Tất cả")
                queryParams.Add($"level={Uri.EscapeDataString(level)}");
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var url = GetApiUrl($"api/hsk/culture-stories?{string.Join("&", queryParams)}");
            var apiList = await _http.GetFromJsonAsync<List<CultureStoryItem>>(url);
            if (apiList != null && apiList.Count > 0)
            {
                return apiList;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureStoryService] API call failed: {ex.Message}. Falling back to sample JSON.");
        }

        // 2. Fallback to sample-data/hsk-culture-stories.json
        var allStories = await GetAllStoriesFromSampleAsync(forceRefresh);
        var filtered = allStories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "Tất cả")
        {
            var catNorm = NormalizeCategory(category);
            filtered = filtered.Where(s => NormalizeCategory(s.Category).Contains(catNorm) || s.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(level) && level != "All" && level != "Tất cả")
        {
            var lvlNorm = level.Trim().ToLowerInvariant();
            filtered = filtered.Where(s => (s.Level != null && s.Level.ToLowerInvariant().Contains(lvlNorm)) ||
                                           (s.HskLevel != null && s.HskLevel.ToLowerInvariant().Contains(lvlNorm)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(sItem =>
                sItem.TitleZh.ToLowerInvariant().Contains(s) ||
                sItem.TitleVi.ToLowerInvariant().Contains(s) ||
                sItem.Pinyin.ToLowerInvariant().Contains(s) ||
                (sItem.SummaryVi != null && sItem.SummaryVi.ToLowerInvariant().Contains(s)));
        }

        return filtered.ToList();
    }

    public async Task<CultureStoryItem?> GetStoryDetailAsync(string idOrSlug)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;

        // 1. Try Backend API
        try
        {
            var url = GetApiUrl($"api/hsk/culture-stories/{Uri.EscapeDataString(idOrSlug)}");
            var item = await _http.GetFromJsonAsync<CultureStoryItem>(url);
            if (item != null && (!string.IsNullOrEmpty(item.TitleZh) || !string.IsNullOrEmpty(item.TitleVi)))
            {
                return item;
            }
        }
        catch { }

        // 2. Fallback to sample-data/hsk-culture-stories.json
        var allStories = await GetAllStoriesFromSampleAsync();
        var norm = idOrSlug.Trim().ToLowerInvariant();

        var found = allStories.FirstOrDefault(s =>
            s.Slug.Equals(norm, StringComparison.OrdinalIgnoreCase) ||
            s.Id.Equals(norm, StringComparison.OrdinalIgnoreCase) ||
            s.StoryId.ToString() == norm);

        return found;
    }

    public async Task<CultureQuizResult> SubmitQuizAsync(int storyId, string? slug, List<int> userAnswers)
    {
        // 1. Try Backend API
        if (storyId > 0)
        {
            try
            {
                var url = GetApiUrl($"api/hsk/culture-stories/{storyId}/quiz");
                var response = await _http.PostAsJsonAsync(url, new { StoryId = storyId, Answers = userAnswers });
                if (response.IsSuccessStatusCode)
                {
                    var res = await response.Content.ReadFromJsonAsync<CultureQuizResult>();
                    if (res != null) return res;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CultureStoryService] API quiz submission failed: {ex.Message}. Local evaluation fallback.");
            }
        }

        // 2. Local Fallback Evaluation
        var story = await GetStoryDetailAsync(slug ?? storyId.ToString());
        var result = new CultureQuizResult
        {
            StoryId = storyId,
            TotalQuestions = story?.Questions?.Count ?? 0
        };

        if (story?.Questions != null && story.Questions.Count > 0)
        {
            int correctCount = 0;
            for (int i = 0; i < story.Questions.Count; i++)
            {
                var q = story.Questions[i];
                result.CorrectIndices.Add(q.CorrectIndex);
                result.Explanations.Add(q.Explanation ?? "");

                bool isCorrect = (i < userAnswers.Count && userAnswers[i] == q.CorrectIndex);
                result.AnswerCorrectness.Add(isCorrect);
                if (isCorrect) correctCount++;
            }

            result.CorrectCount = correctCount;
            result.ScorePercentage = result.TotalQuestions > 0
                ? Math.Round((double)correctCount / result.TotalQuestions * 100, 1)
                : 100;
        }

        return result;
    }

    private async Task<List<CultureStoryItem>> GetAllStoriesFromSampleAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedStories != null && _cachedStories.Count > 0)
            return _cachedStories;

        try
        {
            var list = await _http.GetFromJsonAsync<List<CultureStoryItem>>("sample-data/hsk-culture-stories.json");
            if (list != null)
            {
                _cachedStories = list;
            }
            return _cachedStories ?? new List<CultureStoryItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureStoryService] Error loading sample-data/hsk-culture-stories.json: {ex.Message}");
            return _cachedStories ?? new List<CultureStoryItem>();
        }
    }

    private static string NormalizeCategory(string? cat)
    {
        if (string.IsNullOrWhiteSpace(cat)) return "";
        var c = cat.Trim().ToLowerInvariant();
        if (c.Contains("chengyu") || c.Contains("thành ngữ")) return "chengyu";
        if (c.Contains("history") || c.Contains("lịch sử")) return "history";
        if (c.Contains("custom") || c.Contains("phong tục") || c.Contains("lễ hội")) return "customs";
        if (c.Contains("fable") || c.Contains("ngụ ngôn") || c.Contains("hiện đại")) return "fable";
        return c;
    }
}
