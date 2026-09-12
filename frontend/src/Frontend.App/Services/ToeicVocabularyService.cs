using Blazored.LocalStorage;
using Frontend.App.Models;
using System.Net.Http.Json;

namespace Frontend.App.Services;

public sealed class ToeicVocabularyService
{
    private const string StateKey = "toeic_flashcards_v1";
    private const string CustomVocabKey = "toeic_custom_words_v1";
    private static bool _migrationAttempted = false;

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public ToeicVocabularyService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<List<ToeicVocabItem>> GetVocabulariesAsync()
    {
        List<ToeicVocabItem> words = new();

        // 1. Try loading from database API
        try
        {
            var serverWords = await _httpClient.GetFromJsonAsync<List<ToeicVocabApiDto>>("api/toeic/vocab");
            if (serverWords != null && serverWords.Count > 0)
            {
                words = serverWords.Select(w => new ToeicVocabItem
                {
                    Id = w.Id,
                    Word = w.Word,
                    Ipa = w.Ipa,
                    Meaning = w.Meaning,
                    Example = w.Example ?? "",
                    Topic = w.Topic,
                    IsCustom = w.IsCustom
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicVocabularyService] API load note: {ex.Message}");
        }

        // 2. Fallback to sample-data if database was empty or offline
        if (words.Count == 0)
        {
            try
            {
                var baseWords = await _httpClient.GetFromJsonAsync<List<ToeicVocabItem>>("sample-data/toeic-vocabulary.json");
                if (baseWords != null) words.AddRange(baseWords);
            }
            catch { }
        }

        // 3. Load local custom words (and migrate to server if needed)
        try
        {
            var localCustom = await _localStorage.GetItemAsync<List<ToeicVocabItem>>(CustomVocabKey);
            if (localCustom != null && localCustom.Count > 0)
            {
                foreach (var cw in localCustom)
                {
                    cw.IsCustom = true;
                    if (!words.Any(w => w.Word.Equals(cw.Word, StringComparison.OrdinalIgnoreCase)))
                    {
                        words.Add(cw);
                    }
                }
            }
        }
        catch { }

        return words;
    }

    public async Task<(HashSet<int> Learned, HashSet<int> Again)> GetProgressAsync(List<ToeicVocabItem> allWords)
    {
        HashSet<int> learned = new();
        HashSet<int> againSet = new();

        // Load local state
        ToeicFlashcardState? localState = null;
        try
        {
            localState = await _localStorage.GetItemAsync<ToeicFlashcardState>(StateKey);
            if (localState != null)
            {
                learned = localState.Learned.ToHashSet();
                againSet = localState.Again.ToHashSet();
            }
        }
        catch { }

        // Try load from server
        try
        {
            var serverProgress = await _httpClient.GetFromJsonAsync<ToeicVocabProgressResponseDto>("api/toeic/vocab/progress");
            if (serverProgress != null)
            {
                var srvLearned = serverProgress.LearnedIds?.ToHashSet() ?? new();
                var srvAgain = serverProgress.AgainIds?.ToHashSet() ?? new();

                // Migration if local storage had data not on server
                if (!_migrationAttempted && (learned.Count > 0 || againSet.Count > 0))
                {
                    _migrationAttempted = true;
                    var localCustom = await _localStorage.GetItemAsync<List<ToeicVocabItem>>(CustomVocabKey) ?? new();
                    var customRequests = localCustom.Select(c => new
                    {
                        Word = c.Word,
                        Ipa = c.Ipa,
                        Meaning = c.Meaning,
                        Example = c.Example,
                        Topic = c.Topic
                    }).ToList();

                    _ = _httpClient.PostAsJsonAsync("api/toeic/vocab/progress/migrate", new
                    {
                        LearnedIds = learned.ToList(),
                        AgainIds = againSet.ToList(),
                        CustomWords = customRequests
                    });
                }

                if (srvLearned.Count > 0 || srvAgain.Count > 0)
                {
                    learned = srvLearned;
                    againSet = srvAgain;
                    await SaveLocalStateAsync(learned, againSet);
                }
            }
        }
        catch
        {
            // Offline fallback to localState
        }

        return (learned, againSet);
    }

    public async Task UpdateProgressAsync(int vocabId, string status)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("api/toeic/vocab/progress", new
            {
                VocabularyId = vocabId,
                Status = status // "Learned", "Again", "None"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicVocabularyService] Update progress note: {ex.Message}");
        }
    }

    public async Task<ToeicVocabItem?> CreateCustomVocabAsync(string word, string? ipa, string meaning, string? example, string? topic)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/toeic/vocab", new
            {
                Word = word,
                Ipa = ipa,
                Meaning = meaning,
                Example = example,
                Topic = topic
            });

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<ToeicVocabApiDto>();
                if (created != null)
                {
                    return new ToeicVocabItem
                    {
                        Id = created.Id,
                        Word = created.Word,
                        Ipa = created.Ipa,
                        Meaning = created.Meaning,
                        Example = created.Example ?? "",
                        Topic = created.Topic,
                        IsCustom = true
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicVocabularyService] Create vocab note: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> DeleteCustomVocabAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/toeic/vocab/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task SaveLocalStateAsync(IEnumerable<int> learned, IEnumerable<int> again)
    {
        try
        {
            await _localStorage.SetItemAsync(StateKey, new ToeicFlashcardState
            {
                Learned = learned.ToList(),
                Again = again.ToList()
            });
        }
        catch { }
    }

    private sealed record ToeicVocabApiDto(
        int Id,
        string Word,
        string Ipa,
        string Meaning,
        string? Example,
        string Topic,
        bool IsCustom);

    private sealed record ToeicVocabProgressResponseDto(
        List<int>? LearnedIds,
        List<int>? AgainIds);
}
