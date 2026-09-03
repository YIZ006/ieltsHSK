using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Services;

public class AiGradingService : IAiGradingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AiGradingService> _logger;

    public AiGradingService(IConfiguration config, ILogger<AiGradingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SystemAiSettingsDto GetSavedAiSettings()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "ai_settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Backend.Api", "ai_settings.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ai_settings.json")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    var json = File.ReadAllText(fullPath);
                    var settings = JsonSerializer.Deserialize<SystemAiSettingsDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settings != null)
                    {
                        Console.WriteLine($"[AiGradingService] Loaded ai_settings.json from: {fullPath}");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read ai_settings.json from path {Path}", path);
            }
        }

        Console.WriteLine("[AiGradingService] ai_settings.json not found in any standard path. Using defaults.");
        return new SystemAiSettingsDto();
    }

    private static bool IsCopiedPromptOrEmpty(string essay, string prompt, out int originalWordCount)
    {
        originalWordCount = 0;
        if (string.IsNullOrWhiteSpace(essay)) return true;

        var cleanEssay = Regex.Replace(essay.ToLowerInvariant(), @"[^\w\s]", " ");
        var cleanPrompt = Regex.Replace(prompt.ToLowerInvariant(), @"[^\w\s]", " ");

        var essayWords = cleanEssay.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var promptWords = cleanPrompt.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (essayWords.Length == 0) return true;

        var promptJoined = string.Join(" ", promptWords);
        var essayJoined = string.Join(" ", essayWords);

        // 1. Exact match or essay is a direct substring of prompt
        if (essayJoined == promptJoined || promptJoined.Contains(essayJoined))
        {
            originalWordCount = 0;
            return true;
        }

        // 2. Essay is mostly composed of prompt tokens with almost 0 original writing
        var promptSet = new HashSet<string>(promptWords);
        int nonPromptWords = essayWords.Count(w => !promptSet.Contains(w));
        
        if (essayWords.Length < 50 && ((double)nonPromptWords / essayWords.Length < 0.2))
        {
            originalWordCount = nonPromptWords;
            return true;
        }

        originalWordCount = essayWords.Length;
        return false;
    }

    public async Task<GradeWritingResponse> GradeWritingAsync(GradeWritingRequest request, CancellationToken cancellationToken = default)
    {
        // ── ZERO TOLERANCE COPIED PROMPT CHECK ──
        if (IsCopiedPromptOrEmpty(request.EssayText, request.Prompt, out int origWords))
        {
            Console.WriteLine("[AiGradingService] Detected copied prompt / empty submission. Assigning official Cambridge Band 1.0 penalty.");
            return new GradeWritingResponse
            {
                OverallBand = 1.0,
                TaskResponseBand = 1.0,
                CoherenceBand = 1.0,
                LexicalBand = 1.0,
                GrammarBand = 1.0,
                WordCount = origWords,
                GradedBy = "Cambridge IELTS Strict Rule Engine",
                GeneralFeedback = "BÀI LÀM PHẠM QUY (BAND 1.0): Thí sinh chỉ sao chép lại nguyên văn đề bài và hướng dẫn thi mà không tự viết bất kỳ nội dung mô tả hay phân tích nào. Theo quy định chấm thi chính thức của Cambridge IELTS, toàn bộ từ ngữ sao chép từ đề bài đều BỊ LOẠI BỎ (số từ tự viết = 0 từ) và bị xử điểm liệt Band 1.0 cho tất cả các tiêu chí.",
                Strengths = new List<string> { "Không có điểm mạnh - Thí sinh chưa thực hiện viết bài." },
                Improvements = new List<string>
                {
                    "Tuyệt đối không chép lại nguyên văn đề bài vào phần bài làm.",
                    "Phần Mở bài: Bắt buộc phải Paraphrase (viết lại nội dung đề bài bằng từ đồng nghĩa và cấu trúc câu của chính bạn).",
                    "Phần Tổng quan (Overview): Phải nêu rõ 1-2 xu hướng hoặc điểm thay đổi chính nổi bật nhất.",
                    "Phần Thân bài: Chia thành 2 đoạn chi tiết, mô tả số liệu/địa điểm cụ thể và so sánh có dẫn chứng.",
                    $"Đảm bảo viết tối thiểu {request.MinWords} từ."
                },
                GrammarErrors = new List<WritingGrammarError>
                {
                    new WritingGrammarError
                    {
                        Original = "Toàn bộ bài viết là đề bài sao chép",
                        Suggestion = "Viết lại mở bài: 'The provided maps illustrate the development of Dalton town over a 200-year period from 1815 to 2015.'",
                        Explanation = "Quy chế thi IELTS quy định mọi từ ngữ chép lại nguyên văn từ đề bài sẽ bị gạch bỏ và không được tính điểm từ vựng/ngữ pháp."
                    }
                }
            };
        }

        var settings = GetSavedAiSettings();
        var primaryProvider = (settings.PrimaryWritingProvider ?? "xkiro").ToLowerInvariant();
        Console.WriteLine($"[AiGradingService] Starting Writing Grading. Primary provider: '{primaryProvider}'");

        // 1. Try Primary Configured Provider
        var result = await TryGradeWritingWithProviderAsync(primaryProvider, settings, request, cancellationToken);
        if (result != null) return result;

        // 2. Fallbacks to other configured providers
        var fallbackProviders = new[] { "xkiro", "gemini", "openai", "deepseek" }.Where(p => p != primaryProvider);
        foreach (var fb in fallbackProviders)
        {
            Console.WriteLine($"[AiGradingService] Trying fallback provider: '{fb}'");
            result = await TryGradeWritingWithProviderAsync(fb, settings, request, cancellationToken);
            if (result != null) return result;
        }

        // 3. Environment Variable Gemini fallback
        var envKey = _config["Ai:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("AI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            try
            {
                Console.WriteLine("[AiGradingService] Trying Environment Variable Gemini fallback");
                var aiResult = await CallGeminiWritingGradingAsync(request, envKey, _config["Ai:Model"] ?? "gemini-2.0-flash", cancellationToken);
                if (aiResult != null) return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback Gemini API call failed for Writing grading.");
            }
        }

        // 4. Heuristic NLP Engine fallback
        Console.WriteLine("[AiGradingService] All external AI calls failed or unconfigured. Falling back to local NLP Rubric Engine.");
        var heuristic = EvaluateWritingHeuristic(request);
        heuristic.GradedBy = "NLP Rubric Engine (Offline Fallback)";
        return heuristic;
    }

    public async Task<GradeSpeakingResponse> GradeSpeakingAsync(GradeSpeakingRequest request, CancellationToken cancellationToken = default)
    {
        var settings = GetSavedAiSettings();
        var primaryProvider = (settings.PrimarySpeakingProvider ?? "gemini").ToLowerInvariant();

        // Try Primary Provider
        var result = await TryGradeSpeakingWithProviderAsync(primaryProvider, settings, request, cancellationToken);
        if (result != null) return result;

        // Fallbacks
        var fallbackProviders = new[] { "gemini", "xkiro", "openai", "deepseek" }.Where(p => p != primaryProvider);
        foreach (var fb in fallbackProviders)
        {
            result = await TryGradeSpeakingWithProviderAsync(fb, settings, request, cancellationToken);
            if (result != null) return result;
        }

        // Environment Variable Gemini fallback
        var envKey = _config["Ai:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("AI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            try
            {
                var aiResult = await CallGeminiSpeakingGradingAsync(request, envKey, _config["Ai:Model"] ?? "gemini-2.0-flash", cancellationToken);
                if (aiResult != null) return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback Gemini API call failed for Speaking grading.");
            }
        }

        var heuristic = EvaluateSpeakingHeuristic(request);
        heuristic.GradedBy = "NLP Rubric Engine (Offline Fallback)";
        return heuristic;
    }

    private async Task<GradeWritingResponse?> TryGradeWritingWithProviderAsync(string provider, SystemAiSettingsDto settings, GradeWritingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (provider == "xkiro" && !string.IsNullOrWhiteSpace(settings.Xkiro.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.Xkiro.BaseUrl) ? settings.Xkiro.BaseUrl : "https://api.xkiro.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.Xkiro.DefaultModel) ? settings.Xkiro.DefaultModel : "deepseek-v3";
                Console.WriteLine($"[AiGradingService] Calling xKiro at {baseUrl} with model '{model}'");
                var res = await CallOpenAiCompatibleWritingGradingAsync(request, baseUrl, settings.Xkiro.ApiKey, model, "xKiro AI", cancellationToken);
                if (res != null) return res;
            }
            else if (provider == "gemini" && !string.IsNullOrWhiteSpace(settings.Gemini.ApiKey))
            {
                var model = !string.IsNullOrWhiteSpace(settings.Gemini.DefaultModel) ? settings.Gemini.DefaultModel : "gemini-2.0-flash";
                Console.WriteLine($"[AiGradingService] Calling Google Gemini with model '{model}'");
                var res = await CallGeminiWritingGradingAsync(request, settings.Gemini.ApiKey, model, cancellationToken);
                if (res != null) return res;
            }
            else if (provider == "openai" && !string.IsNullOrWhiteSpace(settings.OpenAi.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.OpenAi.BaseUrl) ? settings.OpenAi.BaseUrl : "https://api.openai.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.OpenAi.DefaultModel) ? settings.OpenAi.DefaultModel : "gpt-4o";
                Console.WriteLine($"[AiGradingService] Calling OpenAI with model '{model}'");
                var res = await CallOpenAiCompatibleWritingGradingAsync(request, baseUrl, settings.OpenAi.ApiKey, model, "OpenAI ChatGPT", cancellationToken);
                if (res != null) return res;
            }
            else if (provider == "deepseek" && !string.IsNullOrWhiteSpace(settings.DeepSeek.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.DeepSeek.BaseUrl) ? settings.DeepSeek.BaseUrl : "https://api.deepseek.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.DeepSeek.DefaultModel) ? settings.DeepSeek.DefaultModel : "deepseek-chat";
                Console.WriteLine($"[AiGradingService] Calling DeepSeek with model '{model}'");
                var res = await CallOpenAiCompatibleWritingGradingAsync(request, baseUrl, settings.DeepSeek.ApiKey, model, "DeepSeek API", cancellationToken);
                if (res != null) return res;
            }
            else
            {
                Console.WriteLine($"[AiGradingService] Provider '{provider}' has no configured API key, skipping.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiGradingService] Exception grading Writing with provider '{provider}': {ex.Message}");
            _logger.LogWarning(ex, "Grading Writing with provider {Provider} failed.", provider);
        }

        return null;
    }

    private async Task<GradeSpeakingResponse?> TryGradeSpeakingWithProviderAsync(string provider, SystemAiSettingsDto settings, GradeSpeakingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (provider == "xkiro" && !string.IsNullOrWhiteSpace(settings.Xkiro.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.Xkiro.BaseUrl) ? settings.Xkiro.BaseUrl : "https://api.xkiro.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.Xkiro.DefaultModel) ? settings.Xkiro.DefaultModel : "deepseek-v3";
                return await CallOpenAiCompatibleSpeakingGradingAsync(request, baseUrl, settings.Xkiro.ApiKey, model, "xKiro AI", cancellationToken);
            }
            else if (provider == "gemini" && !string.IsNullOrWhiteSpace(settings.Gemini.ApiKey))
            {
                var model = !string.IsNullOrWhiteSpace(settings.Gemini.DefaultModel) ? settings.Gemini.DefaultModel : "gemini-2.0-flash";
                return await CallGeminiSpeakingGradingAsync(request, settings.Gemini.ApiKey, model, cancellationToken);
            }
            else if (provider == "openai" && !string.IsNullOrWhiteSpace(settings.OpenAi.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.OpenAi.BaseUrl) ? settings.OpenAi.BaseUrl : "https://api.openai.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.OpenAi.DefaultModel) ? settings.OpenAi.DefaultModel : "gpt-4o-mini";
                return await CallOpenAiCompatibleSpeakingGradingAsync(request, baseUrl, settings.OpenAi.ApiKey, model, "OpenAI ChatGPT", cancellationToken);
            }
            else if (provider == "deepseek" && !string.IsNullOrWhiteSpace(settings.DeepSeek.ApiKey))
            {
                var baseUrl = !string.IsNullOrWhiteSpace(settings.DeepSeek.BaseUrl) ? settings.DeepSeek.BaseUrl : "https://api.deepseek.com/v1";
                var model = !string.IsNullOrWhiteSpace(settings.DeepSeek.DefaultModel) ? settings.DeepSeek.DefaultModel : "deepseek-chat";
                return await CallOpenAiCompatibleSpeakingGradingAsync(request, baseUrl, settings.DeepSeek.ApiKey, model, "DeepSeek API", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Grading Speaking with provider {Provider} failed.", provider);
        }

        return null;
    }

    private string BuildWritingPrompt(GradeWritingRequest request)
    {
        return $@"You are an elite, certified Cambridge IELTS Senior Principal Examiner (Band 8.5 - 9.0 standard).
Your task is to evaluate the following IELTS Writing Task {request.TaskNumber} response with the utmost pedagogical rigor, depth, and actionable precision according to the official Cambridge IELTS 9-Band Descriptors.
DO NOT give generic, superficial feedback. Point out exact lexical flaws, collocations, grammatical bugs, and provide high-impact model upgrades.

=======================================================
[OFFICIAL CAMBRIDGE IELTS SCORING PROTOCOL]:
=======================================================
RULE 0: ZERO TOLERANCE FOR COPIED PROMPT, IRRELEVANT SPAM, OR GIBBERISH (BAND 1.0 - 2.0 RULE):
- Any text copied verbatim or near-verbatim from the question prompt or instructions MUST BE DISCOUNTED (counted as 0 original words).
- If the candidate's essay consists primarily (>70%) of copied prompt text, or has under 25 original words:
  * DO NOT reward grammar (GRA) or vocabulary (LR) for the examiner's own prompt sentences!
  * You MUST assign Band 1.0 for TR, Band 1.0 for CC, Band 1.0 for LR, and Band 1.0 for GRA (Overall Band 1.0).
- If the submission is incomprehensible gibberish or completely off-topic:
  * Assign Band 1.0 - 2.0 across all criteria.

1. TASK ACHIEVEMENT / TASK RESPONSE (TR / TA):
   - Strict Word Count Caps (based ONLY on original student words, excluding copied prompt):
     * < 30 original words: Max Band 1.0 - 2.0.
     * 30 - 70 original words: Max Band 3.0.
     * 71 - 110 words: Max Band 4.0.
     * 111 - 149 words (Task 1) or 111 - 220 words (Task 2): Max Band 5.0.
   - Task 1: Must present a clear, distinct Overview outlining major trends, differences, or stages. If Overview is MISSING, TR is strictly capped at Band 5.0. If data is inaccurate or key features are missed, cap TR at Band 6.0.
   - Task 2: Must fully address ALL parts of the prompt with a clear, sustained position. If the position is unclear, contradictory, or one side of a discussion is ignored, cap TR at Band 5.5.
   - Band 7.0+ requires well-developed main ideas with relevant, extended supporting explanations without overgeneralization.

2. COHERENCE & COHESION (CC):
   - Paragraphing must be logical and distinct (Introduction, separate Body Paragraphs with clear central topics, Conclusion/Overview).
   - Penalize mechanical or repetitive linking phrases (e.g. starting every sentence with 'Firstly, Secondly, Furthermore, Moreover, In addition, In a nutshell').
   - Band 7.0+ requires natural referencing, substitution, and cohesive devices that do not draw undue attention to themselves.

3. LEXICAL RESOURCE (LR) - CRITICAL EVALUATION OF VOCABULARY USAGE:
   - Identify awkward word choices, word-for-word translated Vietnamese phrasing (Viet-glish), repetitive simple words (e.g. 'show', 'increase', 'big', 'good', 'bad', 'people', 'thing'), and informal language.
   - Penalize hollow memorized templates and clichés ('every coin has two sides', 'a plethora of', 'broaden horizon', 'in this modern day and age').
   - Check precision of academic collocations and topic-specific vocabulary.
   - For every essay, pinpoint at least 3-5 weak words/phrases and provide Band 7.5 - 8.5 C1/C2 upgrades with context sentences in 'vocabularyUpgrades'.

4. GRAMMATICAL RANGE & ACCURACY (GRA) - COMPREHENSIVE ERROR ANALYSIS:
   - Scan EVERY sentence for grammatical errors: subject-verb agreement, tense consistency, articles ('a/an/the'), plural suffixes, prepositions, run-on sentences, comma splices, sentence fragments.
   - For every flaw, output an entry in 'grammarErrors' with:
     * 'original': Exact problematic sentence or clause.
     * 'suggestion': Band 8.0+ sophisticated rewrite using advanced grammatical structures (participle clauses, inversion, cleft sentences, nominalisation).
     * 'explanation': Thorough, clear pedagogical explanation in Vietnamese stating the grammatical rule, why it was incorrect, and how to fix it.

5. OVERALL BAND SCORE:
   - Calculate exact average: (TR + CC + LR + GRA) / 4.0.
   - Round to the nearest half band according to official IELTS rules (e.g. 6.25 -> 6.5, 6.75 -> 7.0, 6.125 -> 6.0).

=======================================================
[TASK PROMPT]:
{request.Prompt}

[STUDENT ESSAY]:
{request.EssayText}

[MINIMUM WORD COUNT REQUIRED]: {request.MinWords}
=======================================================

Respond ONLY with valid JSON in this exact structure:
{{
  ""overallBand"": 6.5,
  ""taskResponseBand"": 6.0,
  ""coherenceBand"": 6.5,
  ""lexicalBand"": 7.0,
  ""grammarBand"": 6.5,
  ""wordCount"": 265,
  ""generalFeedback"": ""Nhận xét tổng quan chuyên sâu bằng Tiếng Việt: Phân tích cụ thể lý do chấm từng tiêu chí, đánh giá tư duy lập luận và cấu trúc bài, chỉ rõ điểm nghẽn then chốt cần khắc phục để bứt phá lên Band 7.5+."",
  ""strengths"": [
    ""Điểm mạnh 1 kèm trích dẫn câu hoặc từ vựng tiêu biểu của bài viết."",
    ""Điểm mạnh 2 về cấu trúc đoạn hoặc cách phát triển luận điểm.""
  ],
  ""improvements"": [
    ""Hướng cải thiện 1: Chiến lược cụ thể cho Task Achievement / Response (ví dụ cách viết Overview hoặc dẫn chứng)."",
    ""Hướng cải thiện 2: Chiến lược Coherence & Cohesion (cách dùng từ liên kết tự nhiên và chia đoạn)."",
    ""Hướng cải thiện 3: Mẫu câu và cấu trúc phức nâng cao khuyên dùng cho bài này.""
  ],
  ""vocabularyUpgrades"": [
    {{
      ""originalWord"": ""từ đơn giản hoặc dùng sai trong bài (ví dụ: 'big changes')"",
      ""upgradedWord"": ""từ vựng học thuật C1/C2 (ví dụ: 'profound transformations' / 'radical developments')"",
      ""contextExample"": ""The town underwent profound infrastructural transformations over the two-decade span."",
      ""explanation"": ""Giải thích cách dùng, sắc thái nghĩa và tại sao giúp tăng điểm Lexical Resource.""
    }}
  ],
  ""grammarErrors"": [
    {{
      ""original"": ""câu hoặc cụm từ bị sai trong bài"",
      ""suggestion"": ""câu hoặc cụm từ được sửa lại chuẩn Band 7.5 - 8.5 với cấu trúc nâng cao"",
      ""explanation"": ""Giải thích chi tiết lỗi sai và quy tắc ngữ pháp bằng Tiếng Việt.""
    }}
  ]
}}";
    }

    private string BuildSpeakingPrompt(GradeSpeakingRequest request)
    {
        return $@"You are a certified Cambridge IELTS Senior Speaking Examiner (Band 8.5+ standard).
Evaluate this IELTS Speaking Part {request.PartNumber} response transcript strictly and objectively according to the official IELTS 9-Band Speaking Descriptors.

=======================================================
[EXAMINATION CRITERIA]:
=======================================================
1. FLUENCY AND COHERENCE (FC):
   - Speech rate & flow (WPM: Words Per Minute), pause frequency, hesitation, self-correction, logical linking.
2. LEXICAL RESOURCE (LR):
   - Range of vocabulary, topic-specific idiomatic language, precision, avoidance of basic word repetition.
3. GRAMMATICAL RANGE AND ACCURACY (GRA):
   - Mix of complex structures (conditionals, relative clauses, passive voice), frequency of grammatical errors.
4. PRONUNCIATION (PR):
   - Phonetic clarity and intelligibility indicators based on transcript markers and speech pacing.

=======================================================
[QUESTION]: {request.QuestionText}
[TRANSCRIPT]: {request.Transcript}
[RECORDING DURATION (MS)]: {request.DurationMs}
=======================================================

Respond ONLY with valid JSON in this exact structure:
{{
  ""overallBand"": 6.5,
  ""fluencyBand"": 6.5,
  ""lexicalBand"": 7.0,
  ""grammarBand"": 6.0,
  ""pronunciationBand"": 6.5,
  ""wordCount"": 65,
  ""wpm"": 125,
  ""generalFeedback"": ""Nhận xét chi tiết bằng Tiếng Việt theo 4 tiêu chí IELTS Speaking, đánh giá độ trôi chảy, từ vựng và ngữ pháp."",
  ""strengths"": [
    ""Điểm mạnh về độ phản xạ hoặc từ vựng tự nhiên.""
  ],
  ""improvements"": [
    ""Điểm cần cải thiện để kéo dài câu trả lời và hạn chế ậm ừ.""
  ]
}}";
    }

    private static string CleanJsonMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(7);
        }
        else if (text.StartsWith("```"))
        {
            text = text.Substring(3);
        }
        if (text.EndsWith("```"))
        {
            text = text.Substring(0, text.Length - 3);
        }
        return text.Trim();
    }

    private async Task<GradeWritingResponse?> CallGeminiWritingGradingAsync(GradeWritingRequest request, string apiKey, string model, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var prompt = BuildWritingPrompt(request);

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var res = await client.PostAsync(url, content, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[AiGradingService] Gemini call failed with code {res.StatusCode}: {err}");
            return null;
        }

        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text)) return null;

        var cleanedJson = CleanJsonMarkdown(text);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gradeRes = JsonSerializer.Deserialize<GradeWritingResponse>(cleanedJson, options);
        if (gradeRes != null)
        {
            gradeRes.GradedBy = $"Google Gemini ({model})";
        }
        return gradeRes;
    }

    private async Task<GradeWritingResponse?> CallOpenAiCompatibleWritingGradingAsync(GradeWritingRequest request, string baseUrl, string apiKey, string model, string providerLabel, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var url = $"{baseUrl.TrimEnd('/')}/chat/completions";
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());

        var prompt = BuildWritingPrompt(request);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a certified Cambridge IELTS Senior Principal Examiner. Output strictly valid JSON without markdown formatting." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        Console.WriteLine($"[AiGradingService] Sending POST request to: {url} (Model: {model})");
        var res = await client.PostAsync(url, content, cancellationToken);
        
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[AiGradingService] {providerLabel} failed with status {res.StatusCode}: {err}");
            return null;
        }

        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var rawText = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var cleanedJson = CleanJsonMarkdown(rawText);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gradeRes = JsonSerializer.Deserialize<GradeWritingResponse>(cleanedJson, options);
        if (gradeRes != null)
        {
            gradeRes.GradedBy = $"{providerLabel} ({model})";
            Console.WriteLine($"[AiGradingService] Successfully evaluated Writing with {gradeRes.GradedBy}: Overall Band {gradeRes.OverallBand}");
        }
        return gradeRes;
    }

    private async Task<GradeSpeakingResponse?> CallGeminiSpeakingGradingAsync(GradeSpeakingRequest request, string apiKey, string model, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var prompt = BuildSpeakingPrompt(request);

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var res = await client.PostAsync(url, content, cancellationToken);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text)) return null;

        var cleanedJson = CleanJsonMarkdown(text);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var speakRes = JsonSerializer.Deserialize<GradeSpeakingResponse>(cleanedJson, options);
        if (speakRes != null)
        {
            speakRes.GradedBy = $"Google Gemini ({model})";
        }
        return speakRes;
    }

    private async Task<GradeSpeakingResponse?> CallOpenAiCompatibleSpeakingGradingAsync(GradeSpeakingRequest request, string baseUrl, string apiKey, string model, string providerLabel, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var url = $"{baseUrl.TrimEnd('/')}/chat/completions";
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());

        var prompt = BuildSpeakingPrompt(request);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a certified Cambridge IELTS Senior Examiner. Output strictly valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var res = await client.PostAsync(url, content, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[AiGradingService] {providerLabel} Speaking call failed with {res.StatusCode}: {err}");
            return null;
        }

        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text)) return null;

        var cleanedJson = CleanJsonMarkdown(text);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var speakRes = JsonSerializer.Deserialize<GradeSpeakingResponse>(cleanedJson, options);
        if (speakRes != null)
        {
            speakRes.GradedBy = $"{providerLabel} ({model})";
        }
        return speakRes;
    }

    private static GradeWritingResponse EvaluateWritingHeuristic(GradeWritingRequest request)
    {
        var text = request.EssayText?.Trim() ?? "";
        var words = Regex.Matches(text, @"\b[\w'-]+\b").Select(m => m.Value.ToLowerInvariant()).ToList();
        int wordCount = words.Count;

        if (wordCount < 20)
        {
            return new GradeWritingResponse
            {
                OverallBand = 1.0,
                TaskResponseBand = 1.0,
                CoherenceBand = 1.0,
                LexicalBand = 1.0,
                GrammarBand = 1.0,
                WordCount = wordCount,
                GeneralFeedback = "Bài viết quá ngắn hoặc chưa đủ nội dung để đánh giá chính xác.",
                Strengths = new List<string> { "Đã hoàn thành bước nhập bài." },
                Improvements = new List<string> { $"Cần viết tối thiểu {request.MinWords} từ theo yêu cầu đề bài." }
            };
        }

        double lengthRatio = (double)wordCount / Math.Max(request.MinWords, 150);
        double trScore = lengthRatio switch
        {
            >= 1.1 => 7.5,
            >= 1.0 => 7.0,
            >= 0.85 => 6.0,
            >= 0.70 => 5.5,
            >= 0.50 => 5.0,
            _ => 4.0
        };

        var linkingWords = new HashSet<string> { "however", "moreover", "furthermore", "therefore", "consequently", "in addition", "on the other hand", "firstly", "secondly", "finally", "in conclusion", "overall", "specifically", "for instance", "as a result" };
        int linkingCount = words.Count(w => linkingWords.Contains(w));
        double ccScore = linkingCount switch
        {
            >= 6 => 7.5,
            >= 4 => 6.5,
            >= 2 => 6.0,
            _ => 5.0
        };

        int uniqueCount = words.Distinct().Count();
        double ttr = wordCount > 0 ? (double)uniqueCount / wordCount : 0;
        double lrScore = ttr switch
        {
            >= 0.65 => 7.5,
            >= 0.55 => 7.0,
            >= 0.45 => 6.0,
            >= 0.35 => 5.5,
            _ => 5.0
        };

        var sentences = Regex.Split(text, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        int complexCount = sentences.Count(s => Regex.IsMatch(s, @"\b(although|even though|while|whereas|because|since|if|unless|which|who|whom|whose|that)\b", RegexOptions.IgnoreCase));
        double graScore = (complexCount >= 3) ? 7.0 : (complexCount >= 1 ? 6.0 : 5.0);

        double overall = Math.Round((trScore + ccScore + lrScore + graScore) / 4.0 * 2, MidpointRounding.AwayFromZero) / 2.0;

        return new GradeWritingResponse
        {
            OverallBand = overall,
            TaskResponseBand = trScore,
            CoherenceBand = ccScore,
            LexicalBand = lrScore,
            GrammarBand = graScore,
            WordCount = wordCount,
            GeneralFeedback = $"Bài viết đạt khoảng {wordCount} từ. Bố cục và từ vựng cơ bản đạt yêu cầu bài thi IELTS Writing.",
            Strengths = new List<string> { "Có sử dụng từ nối chuyển tiếp ý.", "Bài viết đúng chủ đề bài thi." },
            Improvements = new List<string> { "Nên mở rộng cấu trúc câu phức và mệnh đề quan hệ.", "Chú ý phát triển ý sâu hơn ở phần thân bài." }
        };
    }

    private static GradeSpeakingResponse EvaluateSpeakingHeuristic(GradeSpeakingRequest request)
    {
        var text = request.Transcript?.Trim() ?? "";
        var words = Regex.Matches(text, @"\b[\w'-]+\b").Select(m => m.Value.ToLowerInvariant()).ToList();
        int wordCount = words.Count;
        double durationSec = Math.Max(1, request.DurationMs / 1000.0);
        int wpm = (int)Math.Round((wordCount / durationSec) * 60.0);

        double fluencyScore = wpm switch
        {
            >= 130 and <= 170 => 7.5,
            >= 110 and <= 190 => 6.5,
            >= 80 => 5.5,
            _ => 4.5
        };

        return new GradeSpeakingResponse
        {
            OverallBand = fluencyScore,
            FluencyBand = fluencyScore,
            LexicalBand = 6.0,
            GrammarBand = 6.0,
            PronunciationBand = 6.5,
            WordCount = wordCount,
            Wpm = wpm,
            GeneralFeedback = $"Tốc độ nói trung bình đạt {wpm} WPM. Bài nói đáp ứng yêu cầu câu hỏi.",
            Strengths = new List<string> { "Phản xạ nhanh, nhịp độ nói tương đối ổn định." },
            Improvements = new List<string> { "Cố gắng diễn đạt câu dài hơn và sử dụng nhiều từ vựng chủ đề hơn." }
        };
    }
}
