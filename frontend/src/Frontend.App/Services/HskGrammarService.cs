using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Frontend.App.Models;

namespace Frontend.App.Services;

public class HskGrammarService
{
    private const string CustomGrammarStorageKey = "hsk_grammar_custom_data";
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private Dictionary<string, HskGrammarLevelData>? _cachedData;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HskGrammarService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<Dictionary<string, HskGrammarLevelData>?> GetAllGrammarDataAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedData != null)
        {
            return _cachedData;
        }

        // 1. Check if Admin saved a custom dataset in LocalStorage
        try
        {
            var customJson = await _localStorage.GetItemAsStringAsync(CustomGrammarStorageKey);
            if (!string.IsNullOrWhiteSpace(customJson))
            {
                // Unwrap if double-serialized string
                if (customJson.StartsWith("\"") && customJson.EndsWith("\""))
                {
                    customJson = JsonSerializer.Deserialize<string>(customJson) ?? customJson;
                }
                var customData = JsonSerializer.Deserialize<Dictionary<string, HskGrammarLevelData>>(customJson, _jsonOptions);
                if (customData != null && customData.Count > 0)
                {
                    _cachedData = customData;
                    return _cachedData;
                }
            }
        }
        catch { }

        // 2. Load default bundled sample-data/hsk-grammar.json
        try
        {
            var data = await _http.GetFromJsonAsync<Dictionary<string, HskGrammarLevelData>>("sample-data/hsk-grammar.json", _jsonOptions);
            if (data != null && data.Count > 0)
            {
                _cachedData = data;
                return _cachedData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskGrammarService] Load error: {ex.Message}");
        }

        return _cachedData ?? new();
    }

    public async Task<HskGrammarLevelData?> GetLevelDataAsync(string level)
    {
        var all = await GetAllGrammarDataAsync();
        if (all == null) return null;
        var lvlKey = level.Replace("HSK", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (all.TryGetValue(lvlKey, out var levelData))
        {
            return levelData;
        }
        return null;
    }

    public async Task<bool> SaveAllGrammarDataAsync(Dictionary<string, HskGrammarLevelData> data)
    {
        try
        {
            _cachedData = data;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await _localStorage.SetItemAsStringAsync(CustomGrammarStorageKey, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskGrammarService] SaveAllGrammarDataAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SaveGrammarPointAsync(string level, string groupName, string categoryName, HskGrammarPointItem item)
    {
        var all = await GetAllGrammarDataAsync() ?? new();
        var lvlKey = level.Replace("HSK", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!all.TryGetValue(lvlKey, out var levelData))
        {
            levelData = new HskGrammarLevelData();
            all[lvlKey] = levelData;
        }

        var group = levelData.Groups.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        if (group == null)
        {
            group = new HskGrammarGroup { Name = groupName };
            levelData.Groups.Add(group);
        }

        var cat = group.Loais.FirstOrDefault(l => string.Equals(l.Name, categoryName, StringComparison.OrdinalIgnoreCase));
        if (cat == null)
        {
            cat = new HskGrammarCategory { Name = categoryName };
            group.Loais.Add(cat);
        }

        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = $"{lvlKey}-custom-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        // Remove from any previous group/category if editing & moving
        foreach (var g in levelData.Groups)
        {
            foreach (var l in g.Loais)
            {
                var existingIdx = l.Items.FindIndex(x => x.Id == item.Id);
                if (existingIdx >= 0)
                {
                    if (g == group && l == cat)
                    {
                        l.Items[existingIdx] = item;
                        return await SaveAllGrammarDataAsync(all);
                    }
                    l.Items.RemoveAt(existingIdx);
                }
            }
        }

        cat.Items.Add(item);
        return await SaveAllGrammarDataAsync(all);
    }

    public async Task<bool> DeleteGrammarPointAsync(string level, string pointId)
    {
        var all = await GetAllGrammarDataAsync();
        if (all == null) return false;
        var lvlKey = level.Replace("HSK", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!all.TryGetValue(lvlKey, out var levelData)) return false;

        bool removed = false;
        foreach (var g in levelData.Groups)
        {
            foreach (var l in g.Loais)
            {
                if (l.Items.RemoveAll(x => x.Id == pointId) > 0)
                {
                    removed = true;
                }
            }
        }

        if (removed)
        {
            return await SaveAllGrammarDataAsync(all);
        }
        return false;
    }

    public async Task<bool> ResetToDefaultAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(CustomGrammarStorageKey);
            _cachedData = null;
            await GetAllGrammarDataAsync(forceRefresh: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HashSet<string>> GetLearnedPointIdsAsync()
    {
        try
        {
            var list = await _localStorage.GetItemAsync<List<string>>("hsk_grammar_learned_ids");
            return list != null ? new HashSet<string>(list) : new HashSet<string>();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    public async Task SaveLearnedPointIdsAsync(HashSet<string> ids)
    {
        try
        {
            await _localStorage.SetItemAsync("hsk_grammar_learned_ids", ids.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskGrammarService] SaveLearnedPointIds error: {ex.Message}");
        }
    }
}
