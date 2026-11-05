using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Frontend.App.Services;

public class SlangVocabItem
{
    public string Id { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string? Hanzi { get; set; }
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public string Category { get; set; } = "number_cipher"; // "number_cipher", "slang_trend", "workplace", "tech_life"
    public string? CategoryVi { get; set; }
    public string? WordType { get; set; }
    public string? Origin { get; set; }
    public string? DialogueZh { get; set; }
    public string? DialogueVi { get; set; }
    public string? DialogueExampleZh
    {
        get => DialogueZh;
        set => DialogueZh = value;
    }
    public string? DialogueExampleVi
    {
        get => DialogueVi;
        set => DialogueVi = value;
    }
    public string? AudioUrl { get; set; }
    public int PopularityScore { get; set; } = 90;
    public List<string> Tags { get; set; } = new();
    public bool IsLearned { get; set; }
}

internal class BackendSlangDto
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public string Category { get; set; } = "slang_trend";
    public string? Origin { get; set; }
    public string? DialogueExampleZh { get; set; }
    public string? DialogueExampleVi { get; set; }
    public string? AudioUrl { get; set; }
    public int PopularityScore { get; set; } = 90;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class HskSlangService
{
    private const string LearnedStorageKey = "learned_slang_terms";
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILocalStorageService _localStorage;
    private List<SlangVocabItem>? _sampleCache;
    private HashSet<string>? _learnedCache;

    // IndexedDB snapshot (dùng chung OfflineDB.vocab_cache như từ vựng IELTS/HSK) để học offline
    public const string OfflineSnapshotKey = "hsk_slang_all";
    private static readonly JsonSerializerOptions _snapshotJson = new(JsonSerializerDefaults.Web);
    private readonly IJSRuntime _js;

    public HskSlangService(HttpClient http, IConfiguration configuration, ILocalStorageService localStorage, IJSRuntime js)
    {
        _http = http;
        _configuration = configuration;
        _localStorage = localStorage;
        _js = js;
    }

    private static bool IsUnfiltered(string? category, string? search)
    {
        var noCategory = string.IsNullOrWhiteSpace(category) || category is "All" or "all" or "Tất cả";
        return noCategory && string.IsNullOrWhiteSpace(search);
    }

    private async Task<List<SlangVocabItem>?> LoadSnapshotAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("OfflineDB.getVocabCache", OfflineSnapshotKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var list = JsonSerializer.Deserialize<List<SlangVocabItem>>(json, _snapshotJson);
            return list is { Count: > 0 } ? list : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveSnapshotAsync(List<SlangVocabItem> list)
    {
        try
        {
            if (list.Count == 0) return;
            var json = JsonSerializer.Serialize(list, _snapshotJson);
            await _js.InvokeAsync<bool>("OfflineDB.saveVocabCache", OfflineSnapshotKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskSlangService] Save offline snapshot failed: {ex.Message}");
        }
    }

    public async Task<bool> IsOfflineReadyAsync() => await LoadSnapshotAsync() != null;

    /// <summary>Tải mới toàn bộ tiếng lóng và lưu vào IndexedDB để học offline.</summary>
    public async Task<bool> DownloadOfflineAsync()
    {
        _sampleCache = null;
        var list = await GetSlangItemsAsync(forceRefresh: true);
        // API không có dữ liệu -> vẫn lưu bản JSON mẫu để dùng offline
        await SaveSnapshotAsync(list);
        return list.Count > 0 && await IsOfflineReadyAsync();
    }

    public async Task DeleteOfflineAsync()
    {
        try { await _js.InvokeAsync<bool>("OfflineDB.saveVocabCache", OfflineSnapshotKey, ""); } catch { }
        _sampleCache = null;
    }

    private string GetApiUrl(string path)
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<List<SlangVocabItem>> GetSlangItemsAsync(
        string? category = null,
        string? search = null,
        bool forceRefresh = false)
    {
        var learnedSet = await GetLearnedSetAsync();

        // 1. Try Live Backend API
        try
        {
            var queryParams = new List<string>();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            queryParams.Add($"_t={ts}");

            if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "all" && category != "Tất cả")
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var url = GetApiUrl($"api/hsk/slang?{string.Join("&", queryParams)}");
            var apiList = await _http.GetFromJsonAsync<List<BackendSlangDto>>(url);
            if (apiList != null && apiList.Count > 0)
            {
                var mapped = apiList.Select(x => new SlangVocabItem
                {
                    Id = x.Id.ToString(),
                    Term = x.Term,
                    Hanzi = x.Term,
                    Pinyin = x.Pinyin,
                    MeaningVi = x.MeaningVi,
                    Category = x.Category,
                    CategoryVi = MapCategoryVi(x.Category),
                    Origin = x.Origin,
                    DialogueZh = x.DialogueExampleZh,
                    DialogueVi = x.DialogueExampleVi,
                    AudioUrl = x.AudioUrl,
                    PopularityScore = x.PopularityScore,
                    IsLearned = learnedSet.Contains(x.Term) || learnedSet.Contains(x.Id.ToString())
                }).ToList();

                // Danh sách đầy đủ (không lọc) -> lưu snapshot IndexedDB để học offline
                if (IsUnfiltered(category, search))
                {
                    await SaveSnapshotAsync(mapped);
                }

                return mapped;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskSlangService] API call failed: {ex.Message}. Falling back to sample JSON.");
        }

        // 2. Fallback to sample-data/hsk-genz-slang.json
        var allSample = await GetAllSampleSlangAsync(forceRefresh);
        var filtered = allSample.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "all" && category != "Tất cả")
        {
            var catNorm = NormalizeCategory(category);
            filtered = filtered.Where(s => NormalizeCategory(s.Category) == catNorm || (s.CategoryVi != null && s.CategoryVi.Contains(category, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(item =>
                item.Term.ToLowerInvariant().Contains(s) ||
                item.Pinyin.ToLowerInvariant().Contains(s) ||
                item.MeaningVi.ToLowerInvariant().Contains(s) ||
                (item.Origin != null && item.Origin.ToLowerInvariant().Contains(s)) ||
                (item.DialogueZh != null && item.DialogueZh.ToLowerInvariant().Contains(s)) ||
                (item.DialogueVi != null && item.DialogueVi.ToLowerInvariant().Contains(s)));
        }

        var result = filtered.ToList();
        foreach (var item in result)
        {
            item.IsLearned = learnedSet.Contains(item.Term) || (!string.IsNullOrEmpty(item.Id) && learnedSet.Contains(item.Id));
        }

        return result;
    }

    public async Task<bool> ToggleLearnedAsync(string termOrId)
    {
        var set = await GetLearnedSetAsync();
        bool newState;
        if (set.Contains(termOrId))
        {
            set.Remove(termOrId);
            newState = false;
        }
        else
        {
            set.Add(termOrId);
            newState = true;
        }

        try
        {
            await _localStorage.SetItemAsync(LearnedStorageKey, set.ToList());
        }
        catch { }

        return newState;
    }

    public async Task<HashSet<string>> GetLearnedSetAsync()
    {
        if (_learnedCache != null) return _learnedCache;

        try
        {
            var list = await _localStorage.GetItemAsync<List<string>>(LearnedStorageKey);
            _learnedCache = list != null ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _learnedCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return _learnedCache;
    }

    private async Task<List<SlangVocabItem>> GetAllSampleSlangAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _sampleCache != null && _sampleCache.Count > 0)
            return _sampleCache;

        try
        {
            var list = await _http.GetFromJsonAsync<List<SlangVocabItem>>("sample-data/hsk-genz-slang.json") ?? new List<SlangVocabItem>();

            // Bổ sung các mục đã lưu offline (từ API) mà JSON mẫu chưa có
            var snapshot = await LoadSnapshotAsync();
            if (snapshot != null)
            {
                foreach (var s in snapshot)
                {
                    if (!list.Any(x => string.Equals(x.Term, s.Term, StringComparison.OrdinalIgnoreCase))) list.Add(s);
                }
            }

            if (list.Count > 0)
            {
                _sampleCache = list;
            }
            return _sampleCache ?? new List<SlangVocabItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskSlangService] Error loading sample-data/hsk-genz-slang.json: {ex.Message}");
            // Mất mạng và chưa có JSON trong cache: dùng snapshot IndexedDB
            var snapshot = await LoadSnapshotAsync();
            if (snapshot != null) _sampleCache = snapshot;
            return _sampleCache ?? new List<SlangVocabItem>();
        }
    }

    private static string NormalizeCategory(string? cat)
    {
        if (string.IsNullOrWhiteSpace(cat)) return "all";
        var c = cat.Trim().ToLowerInvariant();
        if (c.Contains("cipher") || c.Contains("số") || c.Contains("number")) return "cipher";
        if (c.Contains("trend") || c.Contains("slang") || c.Contains("lóng") || c.Contains("douyin")) return "slang";
        if (c.Contains("work") || c.Contains("tech") || c.Contains("công sở") || c.Contains("công nghệ")) return "work_tech";
        return c;
    }

    private static string MapCategoryVi(string? cat)
    {
        var norm = NormalizeCategory(cat);
        return norm switch
        {
            "cipher" => "Mật mã số mạng",
            "slang" => "Hot Trend Douyin/Weibo",
            "work_tech" => "Đời sống & Công nghệ",
            _ => "Tiếng lóng & Xu hướng"
        };
    }
}
