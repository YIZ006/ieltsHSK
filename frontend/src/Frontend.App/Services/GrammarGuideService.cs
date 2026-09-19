using System.Net.Http.Json;
using System.Text.Json;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class GrammarGuideService
{
    private readonly HttpClient _httpClient;
    private List<GrammarGuideSectionDto>? _sectionsCache;
    private readonly Dictionary<string, GrammarGuideTopicDto> _topicCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GrammarGuideService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GrammarGuideSectionDto>> GetSectionsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _sectionsCache != null)
        {
            return _sectionsCache;
        }

        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<GrammarGuideSectionDto>>("data/grammar/sections.json", _jsonOptions);
            _sectionsCache = result ?? new List<GrammarGuideSectionDto>();
            return _sectionsCache;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GrammarGuideService] Failed to load sections: {ex.Message}");
            return _sectionsCache ?? new List<GrammarGuideSectionDto>();
        }
    }

    public async Task<List<GrammarGuideTopicDto>> GetAllTopicsAsync()
    {
        var sections = await GetSectionsAsync();
        return sections.SelectMany(s => s.Topics).ToList();
    }

    public async Task<GrammarGuideTopicDto?> GetTopicBySlugAsync(string slug, bool forceRefresh = false)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var cleanSlug = slug.Trim().ToLowerInvariant();

        if (!forceRefresh && _topicCache.TryGetValue(cleanSlug, out var cachedTopic))
        {
            return cachedTopic;
        }

        try
        {
            var url = $"data/grammar/topics/{cleanSlug}.json";
            var topic = await _httpClient.GetFromJsonAsync<GrammarGuideTopicDto>(url, _jsonOptions);
            if (topic != null)
            {
                _topicCache[cleanSlug] = topic;
            }
            return topic;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GrammarGuideService] Failed to load topic '{cleanSlug}': {ex.Message}");
            return null;
        }
    }

    public async Task<(GrammarGuideTopicDto? Prev, GrammarGuideTopicDto? Next)> GetAdjacentTopicsAsync(string slug)
    {
        var all = await GetAllTopicsAsync();
        var index = all.FindIndex(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (index == -1) return (null, null);

        var prev = index > 0 ? all[index - 1] : null;
        var next = index < all.Count - 1 ? all[index + 1] : null;
        return (prev, next);
    }
}
