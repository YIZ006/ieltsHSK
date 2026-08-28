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

    public async Task<GradeWritingResponse> GradeWritingAsync(GradeWritingRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Ai:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("AI_API_KEY");
        
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var aiResult = await CallGeminiWritingGradingAsync(request, apiKey, cancellationToken);
                if (aiResult != null) return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call failed for Writing grading. Falling back to internal NLP rubric engine.");
            }
        }

        // Heuristic NLP Engine fallback
        return EvaluateWritingHeuristic(request);
    }

    public async Task<GradeSpeakingResponse> GradeSpeakingAsync(GradeSpeakingRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Ai:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("AI_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var aiResult = await CallGeminiSpeakingGradingAsync(request, apiKey, cancellationToken);
                if (aiResult != null) return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call failed for Speaking grading. Falling back to internal NLP rubric engine.");
            }
        }

        return EvaluateSpeakingHeuristic(request);
    }

    private async Task<GradeWritingResponse?> CallGeminiWritingGradingAsync(GradeWritingRequest request, string apiKey, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var model = _config["Ai:Model"] ?? "gemini-2.0-flash";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var prompt = $@"
You are an expert Cambridge IELTS Senior Examiner.
Evaluate the following IELTS Writing Task {request.TaskNumber} essay strictly according to the official IELTS 9-band descriptors.

[TASK PROMPT]:
{request.Prompt}

[STUDENT ESSAY]:
{request.EssayText}

[MINIMUM WORD COUNT REQUIRED]: {request.MinWords}

Respond ONLY with valid JSON in this exact structure:
{{
  ""overallBand"": 6.5,
  ""taskResponseBand"": 6.5,
  ""coherenceBand"": 6.5,
  ""lexicalBand"": 7.0,
  ""grammarBand"": 6.0,
  ""generalFeedback"": ""Detailed overall assessment summarizing strengths and key areas for improvement."",
  ""strengths"": [
    ""Well-structured overview paragraph."",
    ""Accurate use of comparative adjectives and data reporting.""
  ],
  ""improvements"": [
    ""Expand body paragraphs to avoid generalizations."",
    ""Improve comma placement after introductory subordinate clauses.""
  ],
  ""grammarErrors"": [
    {{
      ""original"": ""the number of people are increasing"",
      ""suggestion"": ""the number of people is increasing"",
      ""explanation"": ""'The number of' takes a singular verb.""
    }}
  ],
  ""wordCount"": 175
}}
";

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
            _logger.LogWarning("Gemini API returned status {Status}", res.StatusCode);
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

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GradeWritingResponse>(text, options);
    }

    private async Task<GradeSpeakingResponse?> CallGeminiSpeakingGradingAsync(GradeSpeakingRequest request, string apiKey, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var model = _config["Ai:Model"] ?? "gemini-2.0-flash";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var prompt = $@"
You are an expert Cambridge IELTS Speaking Examiner.
Evaluate this IELTS Speaking Part {request.PartNumber} response transcript strictly according to IELTS Speaking 9-band criteria.

[QUESTION]: {request.QuestionText}
[TRANSCRIPT]: {request.Transcript}
[RECORDING DURATION (MS)]: {request.DurationMs}

Respond ONLY with valid JSON in this exact structure:
{{
  ""overallBand"": 6.5,
  ""fluencyBand"": 6.5,
  ""lexicalBand"": 7.0,
  ""grammarBand"": 6.0,
  ""pronunciationBand"": 6.5,
  ""generalFeedback"": ""Detailed evaluation on fluency, vocabulary, and grammar range."",
  ""wordCount"": 65,
  ""wpm"": 125,
  ""strengths"": [
    ""Answer directly addresses the prompt."",
    ""Uses cohesive linking phrases naturally.""
  ],
  ""improvements"": [
    ""Reduce hesitation before expressing complex opinions."",
    ""Vary sentence structures with conditional or relative clauses.""
  ]
}}
";

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

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GradeSpeakingResponse>(text, options);
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
                OverallBand = 2.0,
                TaskResponseBand = 2.0,
                CoherenceBand = 2.0,
                LexicalBand = 2.0,
                GrammarBand = 2.0,
                WordCount = wordCount,
                GeneralFeedback = "Bài viết quá ngắn hoặc chưa đủ nội dung để đánh giá chính xác.",
                Strengths = new List<string> { "Đã hoàn thành bước nhập bài." },
                Improvements = new List<string> { $"Cần viết tối thiểu {request.MinWords} từ theo yêu cầu đề bài." }
            };
        }

        // 1. Task Response
        double lengthRatio = (double)wordCount / Math.Max(request.MinWords, 150);
        double trScore = lengthRatio switch
        {
            >= 1.1 => 7.5,
            >= 1.0 => 7.0,
            >= 0.85 => 6.5,
            >= 0.70 => 5.5,
            >= 0.50 => 4.5,
            _ => 3.5
        };

        // 2. Coherence & Cohesion
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var cohesiveDevices = new[] { "furthermore", "moreover", "in addition", "on the other hand", "however", "consequently", "therefore", "overall", "in conclusion", "for instance", "for example", "specifically", "as a result", "whereas", "while" };
        int cohesiveCount = cohesiveDevices.Count(d => text.Contains(d, StringComparison.OrdinalIgnoreCase));
        double ccScore = 5.0;
        if (paragraphs.Length >= (request.TaskNumber == 1 ? 3 : 4) && cohesiveCount >= 3) ccScore = 7.0;
        else if (paragraphs.Length >= 2 && cohesiveCount >= 2) ccScore = 6.0;

        // 3. Lexical Resource
        var uniqueWords = new HashSet<string>(words);
        double ttr = (double)uniqueWords.Count / Math.Max(wordCount, 1);
        var academicVocab = new[] { "significant", "substantial", "demonstrate", "illustrate", "fluctuate", "approximately", "dramatically", "crucial", "essential", "phenomenon", "predominant", "accelerate", "stabilize", "perspective", "proponent", "detrimental" };
        int academicCount = academicVocab.Count(w => words.Contains(w));
        double lrScore = (ttr >= 0.55 && academicCount >= 3) ? 7.5 : (ttr >= 0.45 || academicCount >= 1 ? 6.5 : 5.5);

        // 4. Grammar Range & Accuracy
        var sentences = Regex.Split(text, @"[.!?]+").Where(s => s.Trim().Length > 5).ToList();
        double avgSentenceLen = sentences.Count > 0 ? (double)wordCount / sentences.Count : 10;
        double graScore = (avgSentenceLen >= 15 && sentences.Count >= 6) ? 7.0 : (avgSentenceLen >= 10 ? 6.0 : 5.0);

        double overall = RoundHalfBand((trScore + ccScore + lrScore + graScore) / 4.0);

        var strengths = new List<string>();
        if (wordCount >= request.MinWords) strengths.Add($"Đạt chuẩn độ dài yêu cầu ({wordCount}/{request.MinWords} từ).");
        if (paragraphs.Length >= 3) strengths.Add("Bố cục chia đoạn rõ ràng, có mở bài, thân bài và kết luận.");
        if (cohesiveCount >= 2) strengths.Add("Sử dụng các từ nối logic (linking words) để kết nối ý.");

        var improvements = new List<string>();
        if (wordCount < request.MinWords) improvements.Add($"Cần bổ sung thêm luận điểm để đạt tối thiểu {request.MinWords} từ.");
        if (academicCount < 2) improvements.Add("Nâng cao vốn từ vựng học thuật C1/C2 (Academic Collocations) để tăng điểm Lexical Resource.");
        if (paragraphs.Length < 3) improvements.Add("Nên chia bài viết thành tối thiểu 3-4 đoạn văn rõ ràng.");

        return new GradeWritingResponse
        {
            OverallBand = overall,
            TaskResponseBand = trScore,
            CoherenceBand = ccScore,
            LexicalBand = lrScore,
            GrammarBand = graScore,
            WordCount = wordCount,
            GeneralFeedback = $"Bài viết đạt Band {overall:0.0}. Khả năng triển khai ý tương đối tốt, bố cục {paragraphs.Length} đoạn văn. Cần tiếp tục đa dạng cấu trúc câu phức và từ vựng chuyên sâu.",
            Strengths = strengths,
            Improvements = improvements,
            GrammarErrors = new List<WritingGrammarError>()
        };
    }

    private static GradeSpeakingResponse EvaluateSpeakingHeuristic(GradeSpeakingRequest request)
    {
        var text = request.Transcript?.Trim() ?? "";
        var words = Regex.Matches(text, @"\b[\w'-]+\b").Select(m => m.Value.ToLowerInvariant()).ToList();
        int wordCount = words.Count;
        double sec = Math.Max(request.DurationMs / 1000.0, 1.0);
        int wpm = (int)Math.Round((wordCount / sec) * 60.0);

        if (wordCount < 5)
        {
            return new GradeSpeakingResponse
            {
                OverallBand = 3.0,
                FluencyBand = 3.0,
                LexicalBand = 3.0,
                GrammarBand = 3.0,
                PronunciationBand = 3.0,
                WordCount = wordCount,
                Wpm = wpm,
                GeneralFeedback = "Thời lượng nói hoặc số lượng từ quá ngắn để đánh giá đầy đủ. Cần trả lời mở rộng hơn với ví dụ cụ thể.",
                Strengths = new List<string> { "Đã ghi nhận bản ghi âm." },
                Improvements = new List<string> { "Nói liên tục và mở rộng câu trả lời từ 3-5 câu mỗi câu hỏi." }
            };
        }

        double fcScore = wpm switch
        {
            >= 125 => 7.5,
            >= 100 => 6.5,
            >= 75 => 5.5,
            _ => 4.5
        };

        var unique = new HashSet<string>(words);
        double ttr = (double)unique.Count / Math.Max(wordCount, 1);
        double lrScore = ttr >= 0.65 ? 7.0 : (ttr >= 0.5 ? 6.0 : 5.0);
        double graScore = wordCount >= 30 ? 6.5 : 5.5;
        double prScore = fcScore;

        double overall = RoundHalfBand((fcScore + lrScore + graScore + prScore) / 4.0);

        return new GradeSpeakingResponse
        {
            OverallBand = overall,
            FluencyBand = fcScore,
            LexicalBand = lrScore,
            GrammarBand = graScore,
            PronunciationBand = prScore,
            WordCount = wordCount,
            Wpm = wpm,
            GeneralFeedback = $"Tốc độ nói {wpm} từ/phút. Phản xạ và độ trôi chảy đạt mức Band {overall:0.0}.",
            Strengths = new List<string>
            {
                $"Tốc độ nói ổn định ({wpm} WPM).",
                "Trả lời đúng trọng tâm câu hỏi của giám khảo."
            },
            Improvements = new List<string>
            {
                "Sử dụng thêm các thành ngữ (idiomatic expressions) và từ vựng theo chủ đề.",
                "Tăng cường nối âm và ngữ điệu tự nhiên (intonation)."
            }
        };
    }

    private static double RoundHalfBand(double score)
    {
        double floor = Math.Floor(score);
        double frac = score - floor;
        if (frac >= 0.75) return floor + 1.0;
        if (frac >= 0.25) return floor + 0.5;
        return floor;
    }
}
