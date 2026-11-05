using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.App.Services;

public class ChengyuExample
{
    public string Zh { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
}

public class ChengyuCharItem
{
    public string Char { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string HanViet { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
}

public class WechatWordToken
{
    public string Word { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public bool IsIdiom { get; set; }
}

public class WechatMessageItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Sender { get; set; } = "left"; // "left" or "right"
    public string SpeakerName { get; set; } = string.Empty;
    public string Zh { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
    public string? HighlightIdiom { get; set; }
    public List<WechatWordToken> Words { get; set; } = new();

    /// <summary>
    /// UI-only state: whether the dropdown translation is currently expanded
    /// </summary>
    public bool IsTranslationOpen { get; set; } = true;
}

public class WechatChatModel
{
    public string ContactName { get; set; } = "珍肺";
    public string RightSpeakerName { get; set; } = "阿明";
    public string Timestamp { get; set; } = "16:32";
    public string AvatarLeft { get; set; } = "/images/idioms/avatar-wet-cat.jpg";
    public string AvatarRight { get; set; } = "/images/idioms/avatar-crying-cat.jpg";
    public List<WechatMessageItem> Messages { get; set; } = new();
}

public class ChengyuItem
{
    public string Id { get; set; } = string.Empty;
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string HanViet { get; set; } = string.Empty;
    public string ShortMeaningVi { get; set; } = string.Empty;
    public string HskLevel { get; set; } = "HSK 5"; // "HSK 4", "HSK 5", "HSK 6"
    public string Category { get; set; } = "fable_story";
    public string? CategoryVi { get; set; }
    public string MeaningVi { get; set; } = string.Empty;
    public string? OriginStory { get; set; }
    public List<ChengyuCharItem> CharBreakdown { get; set; } = new();
    public List<string> Synonyms { get; set; } = new();
    public List<string> Antonyms { get; set; } = new();
    public List<ChengyuExample> HskExamples { get; set; } = new();
    public WechatChatModel? WechatChat { get; set; }
    public int PopularityScore { get; set; } = 95;
    public List<string> Tags { get; set; } = new();

    // Client state
    public bool IsLearned { get; set; }
    public bool IsFavorite { get; set; }
}

public class HskChengyuService
{
    private const string LearnedStorageKey = "learned_chengyu_items";
    private const string FavoriteStorageKey = "favorite_chengyu_items";
    private const string CustomItemsStorageKey = "custom_chengyu_items";

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILocalStorageService _localStorage;
    private List<ChengyuItem>? _cache;
    private HashSet<string>? _learnedCache;
    private HashSet<string>? _favoriteCache;

    // IndexedDB snapshot (dùng chung OfflineDB.vocab_cache như từ vựng IELTS/HSK) để học offline
    public const string OfflineSnapshotKey = "hsk_chengyu_all";
    private static readonly JsonSerializerOptions _snapshotJson = new(JsonSerializerDefaults.Web);
    private readonly IJSRuntime _js;

    public HskChengyuService(HttpClient http, IConfiguration configuration, ILocalStorageService localStorage, IJSRuntime js)
    {
        _http = http;
        _configuration = configuration;
        _localStorage = localStorage;
        _js = js;
    }

    private async Task<List<ChengyuItem>?> LoadSnapshotAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("OfflineDB.getVocabCache", OfflineSnapshotKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var list = JsonSerializer.Deserialize<List<ChengyuItem>>(json, _snapshotJson);
            return list is { Count: > 0 } ? list : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveSnapshotAsync(List<ChengyuItem> list)
    {
        try
        {
            if (list.Count == 0) return;
            var json = JsonSerializer.Serialize(list, _snapshotJson);
            await _js.InvokeAsync<bool>("OfflineDB.saveVocabCache", OfflineSnapshotKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskChengyuService] Save offline snapshot failed: {ex.Message}");
        }
    }

    public async Task<bool> IsOfflineReadyAsync() => await LoadSnapshotAsync() != null;

    /// <summary>Tải mới toàn bộ thành ngữ và lưu vào IndexedDB để học offline.</summary>
    public async Task<bool> DownloadOfflineAsync()
    {
        var list = await GetAllIdiomsAsync(forceRefresh: true);
        await SaveSnapshotAsync(list);
        return list.Count > 0 && await IsOfflineReadyAsync();
    }

    public async Task DeleteOfflineAsync()
    {
        try { await _js.InvokeAsync<bool>("OfflineDB.saveVocabCache", OfflineSnapshotKey, ""); } catch { }
        _cache = null;
    }

    private string GetApiUrl(string path)
    {
        var baseUrl = _configuration["BackendApi:BaseUrl"] ?? "http://localhost:5101/";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<HashSet<string>> GetLearnedSetAsync()
    {
        if (_learnedCache != null) return _learnedCache;
        try
        {
            var stored = await _localStorage.GetItemAsync<List<string>>(LearnedStorageKey);
            _learnedCache = stored != null ? new HashSet<string>(stored) : new HashSet<string>();
        }
        catch
        {
            _learnedCache = new HashSet<string>();
        }
        return _learnedCache;
    }

    public async Task<HashSet<string>> GetFavoriteSetAsync()
    {
        if (_favoriteCache != null) return _favoriteCache;
        try
        {
            var stored = await _localStorage.GetItemAsync<List<string>>(FavoriteStorageKey);
            _favoriteCache = stored != null ? new HashSet<string>(stored) : new HashSet<string>();
        }
        catch
        {
            _favoriteCache = new HashSet<string>();
        }
        return _favoriteCache;
    }

    public async Task ToggleLearnedAsync(string idOrHanzi)
    {
        var set = await GetLearnedSetAsync();
        if (set.Contains(idOrHanzi)) set.Remove(idOrHanzi);
        else set.Add(idOrHanzi);

        _learnedCache = set;
        try { await _localStorage.SetItemAsync(LearnedStorageKey, set.ToList()); } catch { }

        if (_cache != null)
        {
            var item = _cache.FirstOrDefault(x => x.Id == idOrHanzi || x.Hanzi == idOrHanzi);
            if (item != null) item.IsLearned = set.Contains(idOrHanzi);
        }
    }

    public async Task ToggleFavoriteAsync(string idOrHanzi)
    {
        var set = await GetFavoriteSetAsync();
        if (set.Contains(idOrHanzi)) set.Remove(idOrHanzi);
        else set.Add(idOrHanzi);

        _favoriteCache = set;
        try { await _localStorage.SetItemAsync(FavoriteStorageKey, set.ToList()); } catch { }

        if (_cache != null)
        {
            var item = _cache.FirstOrDefault(x => x.Id == idOrHanzi || x.Hanzi == idOrHanzi);
            if (item != null) item.IsFavorite = set.Contains(idOrHanzi);
        }
    }

    public async Task<List<ChengyuItem>> GetAllIdiomsAsync(bool forceRefresh = false)
    {
        if (_cache != null && !forceRefresh) return _cache;

        var learnedSet = await GetLearnedSetAsync();
        var favSet = await GetFavoriteSetAsync();

        // 1. Load bundled sample-data/hsk-chengyu.json first (instant & complete 25+ idioms)
        var list = new List<ChengyuItem>();
        try
        {
            list = await _http.GetFromJsonAsync<List<ChengyuItem>>("sample-data/hsk-chengyu.json") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskChengyuService] Error loading local sample-data: {ex.Message}");
        }

        // 2. Merge any new/updated items from Live Backend API if available
        var apiOk = false;
        try
        {
            var url = GetApiUrl("api/hsk/idioms");
            var apiList = await _http.GetFromJsonAsync<List<ChengyuItem>>(url);
            if (apiList != null && apiList.Count > 0)
            {
                apiOk = true;
                foreach (var apiItem in apiList)
                {
                    var idx = list.FindIndex(x => x.Id == apiItem.Id || x.Hanzi == apiItem.Hanzi);
                    if (idx >= 0)
                    {
                        // Preserve rich local CharBreakdown / Words if backend copy is older
                        if ((apiItem.CharBreakdown == null || apiItem.CharBreakdown.Count == 0) && list[idx].CharBreakdown.Count > 0)
                            apiItem.CharBreakdown = list[idx].CharBreakdown;
                        if (string.IsNullOrWhiteSpace(apiItem.ShortMeaningVi))
                            apiItem.ShortMeaningVi = list[idx].ShortMeaningVi;
                        if (apiItem.WechatChat != null && list[idx].WechatChat != null)
                        {
                            for (int m = 0; m < Math.Min(apiItem.WechatChat.Messages.Count, list[idx].WechatChat!.Messages.Count); m++)
                            {
                                if (apiItem.WechatChat.Messages[m].Words.Count == 0 && list[idx].WechatChat!.Messages[m].Words.Count > 0)
                                    apiItem.WechatChat.Messages[m].Words = list[idx].WechatChat!.Messages[m].Words;
                            }
                        }
                        list[idx] = apiItem;
                    }
                    else
                    {
                        list.Add(apiItem);
                    }
                }
            }
        }
        catch
        {
            // Ignore backend offline fallback
        }

        // 2b. Offline snapshot (IndexedDB): API lỗi/mất mạng thì bổ sung các mục đã lưu mà JSON mẫu chưa có;
        //     API thành công thì cập nhật lại snapshot cho lần học offline sau.
        if (!apiOk)
        {
            var snapshot = await LoadSnapshotAsync();
            if (snapshot != null)
            {
                foreach (var s in snapshot)
                {
                    if (!list.Any(x => x.Id == s.Id || x.Hanzi == s.Hanzi)) list.Add(s);
                }
            }
        }
        else
        {
            await SaveSnapshotAsync(list);
        }

        // 3. Merge any local custom items added by admin in LocalStorage
        try
        {
            var customItems = await _localStorage.GetItemAsync<List<ChengyuItem>>(CustomItemsStorageKey);
            if (customItems != null && customItems.Count > 0)
            {
                foreach (var c in customItems)
                {
                    var idx = list.FindIndex(x => x.Id == c.Id || x.Hanzi == c.Hanzi);
                    if (idx >= 0) list[idx] = c;
                    else list.Insert(0, c);
                }
            }
        }
        catch { }

        foreach (var item in list)
        {
            item.IsLearned = learnedSet.Contains(item.Id) || learnedSet.Contains(item.Hanzi);
            item.IsFavorite = favSet.Contains(item.Id) || favSet.Contains(item.Hanzi);
        }

        _cache = list;
        return _cache;
    }

    public async Task<List<ChengyuItem>> GetFilteredIdiomsAsync(
        string? level = null,
        string? category = null,
        string? search = null,
        bool hideLearned = false)
    {
        var all = await GetAllIdiomsAsync();
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(level) && level != "all" && level != "All" && level != "Tất cả")
        {
            query = query.Where(x => x.HskLevel.Equals(level, StringComparison.OrdinalIgnoreCase) ||
                                     x.HskLevel.Replace(" ", "").Equals(level.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all" && category != "All" && category != "Tất cả")
        {
            query = query.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                                     (x.CategoryVi != null && x.CategoryVi.Contains(category, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Hanzi.ToLowerInvariant().Contains(s) ||
                x.Pinyin.ToLowerInvariant().Contains(s) ||
                x.HanViet.ToLowerInvariant().Contains(s) ||
                x.MeaningVi.ToLowerInvariant().Contains(s) ||
                (x.OriginStory != null && x.OriginStory.ToLowerInvariant().Contains(s)) ||
                (x.WechatChat != null && x.WechatChat.Messages.Any(m => m.Zh.ToLowerInvariant().Contains(s) || m.Vi.ToLowerInvariant().Contains(s))));
        }

        if (hideLearned)
        {
            query = query.Where(x => !x.IsLearned);
        }

        return query.ToList();
    }

    public async Task<ChengyuItem?> GetByIdAsync(string id)
    {
        var all = await GetAllIdiomsAsync();
        return all.FirstOrDefault(x => x.Id == id || x.Hanzi == id);
    }

    public async Task<bool> SaveIdiomAsync(ChengyuItem item)
    {
        try
        {
            // 1. Try Backend API
            try
            {
                var url = GetApiUrl(string.IsNullOrEmpty(item.Id) ? "api/admin/hsk/idioms" : $"api/admin/hsk/idioms/{item.Id}");
                var res = await _http.PostAsJsonAsync(url, item);
                if (res.IsSuccessStatusCode)
                {
                    await GetAllIdiomsAsync(forceRefresh: true);
                    return true;
                }
            }
            catch { }

            // 2. Client-side LocalStorage save
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = "chengyu-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            }

            var customList = await _localStorage.GetItemAsync<List<ChengyuItem>>(CustomItemsStorageKey) ?? new();
            var existingIndex = customList.FindIndex(x => x.Id == item.Id || x.Hanzi == item.Hanzi);
            if (existingIndex >= 0)
            {
                customList[existingIndex] = item;
            }
            else
            {
                customList.Insert(0, item);
            }
            await _localStorage.SetItemAsync(CustomItemsStorageKey, customList);

            if (_cache != null)
            {
                var idx = _cache.FindIndex(x => x.Id == item.Id || x.Hanzi == item.Hanzi);
                if (idx >= 0) _cache[idx] = item;
                else _cache.Insert(0, item);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskChengyuService] Error saving idiom: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteIdiomAsync(string id)
    {
        try
        {
            // Try backend API
            try
            {
                var url = GetApiUrl($"api/admin/hsk/idioms/{id}");
                await _http.DeleteAsync(url);
            }
            catch { }

            var customList = await _localStorage.GetItemAsync<List<ChengyuItem>>(CustomItemsStorageKey) ?? new();
            customList.RemoveAll(x => x.Id == id);
            await _localStorage.SetItemAsync(CustomItemsStorageKey, customList);

            if (_cache != null)
            {
                _cache.RemoveAll(x => x.Id == id);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SyncChengyuR2Result?> PushToR2Async(string? explicitJson = null)
    {
        try
        {
            string jsonToSend;
            if (!string.IsNullOrWhiteSpace(explicitJson))
            {
                jsonToSend = explicitJson;
            }
            else
            {
                var all = await GetAllIdiomsAsync();
                jsonToSend = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
            }

            var url = GetApiUrl("api/admin/hsk/idioms/sync-r2");
            var res = await _http.PostAsJsonAsync(url, new { JsonContent = jsonToSend });
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<SyncChengyuR2Result>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HskChengyuService] PushToR2 error: {ex.Message}");
        }
        return null;
    }
}

public class SyncChengyuR2Result
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public string R2Url { get; set; } = string.Empty;
    public bool IsUploaded { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double SizeKb { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
