using Blazored.LocalStorage;
using Frontend.App.Models;

using System.Net.Http.Json;

namespace Frontend.App.Services;

public sealed class ExamSubmissionService(ILocalStorageService localStorage, HttpClient httpClient)
{
    private const string StorageKey = "ielts-exam-submissions";

    public async Task<IeltsSubmissionRecord> SaveAsync(IeltsSubmissionRecord submission)
    {
        submission.Id = string.IsNullOrWhiteSpace(submission.Id) ? Guid.NewGuid().ToString("N") : submission.Id;
        submission.SubmittedAt = submission.SubmittedAt == default ? DateTimeOffset.UtcNow : submission.SubmittedAt;

        var submissions = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        var index = submissions.FindIndex(item => 
            item.Id == submission.Id || 
            (!string.IsNullOrEmpty(submission.SessionId) && item.SessionId == submission.SessionId && string.Equals(item.Skill, submission.Skill, StringComparison.OrdinalIgnoreCase)));
        if (index >= 0) submissions[index] = submission;
        else submissions.Insert(0, submission);

        await localStorage.SetItemAsync(StorageKey, submissions);
        return submission;
    }

    public async Task<TestSubmissionDbResponseDto?> SaveToDbAsync(
        string skill,
        string examUrl,
        string sessionId,
        double bandScore,
        int correctCount,
        int totalCount,
        string? detailsJson = null,
        string status = "Pending",
        string? examTitle = null,
        string? studentName = null,
        int? attemptNumber = null,
        string? audioKey = null)
    {
        var request = new 
        {
            Skill = skill,
            ExamUrl = examUrl,
            ExamTitle = examTitle,
            SessionId = sessionId,
            StudentName = studentName,
            AttemptNumber = attemptNumber,
            BandScore = bandScore,
            CorrectCount = correctCount,
            TotalCount = totalCount,
            DetailsJson = detailsJson,
            Status = status,
            AudioKey = audioKey
        };
        try
        {
            var res = await httpClient.PostAsJsonAsync("api/test-submissions", request);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<TestSubmissionDbResponseDto>();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"[ExamSubmissionService] API returned error ({res.StatusCode}): {err}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExamSubmissionService] Error saving submission to DB: {ex.Message}");
        }
        return null;
    }

    public async Task<List<IeltsSubmissionRecord>> GetAllAsync()
    {
        var local = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        try
        {
            var queryParams = new List<string>();
            var token = await localStorage.GetItemAsync<string>("authToken");
            int? userId = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                var claims = CustomAuthStateProvider.ParseClaimsFromJwt(token);
                var sub = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid" || c.Type == "id")?.Value;
                if (int.TryParse(sub, out var parsedUid) && parsedUid > 0) userId = parsedUid;
            }

            var profile = await localStorage.GetItemAsync<UserProfile>("user_profile");
            if (!userId.HasValue && profile?.Id > 0) userId = profile.Id.Value;

            if (userId.HasValue) queryParams.Add($"userId={userId.Value}");
            if (!string.IsNullOrWhiteSpace(profile?.FullName)) queryParams.Add($"studentName={Uri.EscapeDataString(profile.FullName)}");
            else if (!string.IsNullOrWhiteSpace(profile?.DisplayName)) queryParams.Add($"studentName={Uri.EscapeDataString(profile.DisplayName)}");
            queryParams.Add($"_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

            var url = $"api/test-submissions/sync?{string.Join("&", queryParams)}";
            var serverItems = await httpClient.GetFromJsonAsync<List<TestSubmissionSyncDto>>(url);
            if (serverItems != null && serverItems.Count > 0)
            {
                bool hasChanges = false;
                foreach (var srv in serverItems)
                {
                    var match = FindLocalMatch(local, srv);

                    if (match != null)
                    {
                        match.Id = srv.Id.ToString();
                        match.SubmittedAt = srv.SubmittedAt;

                        if (srv.Status == "Graded" || srv.Status == "Scored")
                        {
                            match.Status = srv.Status;
                            match.BandScore = srv.BandScore;
                            match.TeacherFeedback = srv.TeacherFeedback;
                            match.CorrectCount = srv.CorrectCount;
                            match.TotalQuestions = srv.TotalCount;
                            hasChanges = true;
                        }

                        // Kèm theo báo cáo 4 tiêu chí (scoreReport) lưu trong DetailsJson khi giáo viên chấm
                        if (!string.IsNullOrWhiteSpace(srv.DetailsJson)
                            && (string.Equals(match.Skill, "Writing", StringComparison.OrdinalIgnoreCase) || string.Equals(match.Skill, "Speaking", StringComparison.OrdinalIgnoreCase)))
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(srv.DetailsJson);
                                if (doc.RootElement.TryGetProperty("scoreReport", out var scoreElem) && scoreElem.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    var report = System.Text.Json.JsonSerializer.Deserialize<IeltsScoreReport>(scoreElem.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (report != null)
                                    {
                                        match.Score = report;
                                        hasChanges = true;
                                    }
                                }
                            }
                            catch
                            {
                                // DetailsJson không parse được là non-fatal, bỏ qua
                            }
                        }
                    }
                    else
                    {
                        var newRecord = new IeltsSubmissionRecord
                        {
                            Id = srv.Id.ToString(),
                            Skill = srv.Skill,
                            ExamUrl = srv.ExamUrl,
                            ExamTitle = !string.IsNullOrWhiteSpace(srv.ExamTitle) ? srv.ExamTitle : (!string.IsNullOrWhiteSpace(srv.ExamUrl) ? Path.GetFileNameWithoutExtension(srv.ExamUrl) : srv.Skill),
                            SessionId = srv.SessionId,
                            BandScore = srv.BandScore,
                            CorrectCount = srv.CorrectCount,
                            TotalQuestions = srv.TotalCount,
                            Status = srv.Status,
                            TeacherFeedback = srv.TeacherFeedback,
                            SubmittedAt = srv.SubmittedAt
                        };
                        
                        if (!string.IsNullOrWhiteSpace(srv.DetailsJson)
                            && (string.Equals(newRecord.Skill, "Writing", StringComparison.OrdinalIgnoreCase) || string.Equals(newRecord.Skill, "Speaking", StringComparison.OrdinalIgnoreCase)))
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(srv.DetailsJson);
                                if (doc.RootElement.TryGetProperty("scoreReport", out var scoreElem) && scoreElem.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    var report = System.Text.Json.JsonSerializer.Deserialize<IeltsScoreReport>(scoreElem.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (report != null)
                                    {
                                        newRecord.Score = report;
                                    }
                                }
                            }
                            catch {}
                        }

                        local.Add(newRecord);
                        hasChanges = true;
                    }
                }

                // Dọn dẹp bản ghi trùng lặp trong local: ưu tiên bản ghi có điểm/Graded/Scored
                var deduplicated = local
                    .GroupBy(x => !string.IsNullOrEmpty(x.Id) && int.TryParse(x.Id, out _) ? x.Id : $"{x.Skill}_{NormalizeUrl(x.ExamUrl)}_{x.SessionId}")
                    .Select(g => g.OrderByDescending(x => x.Status == "Graded" || x.Status == "Scored" ? 1 : 0).ThenByDescending(x => x.SubmittedAt).First())
                    .OrderByDescending(x => x.SubmittedAt)
                    .ToList();

                if (deduplicated.Count != local.Count)
                {
                    local = deduplicated;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await localStorage.SetItemAsync(StorageKey, local);
                }
            }
        }
        catch
        {
            // Sync failure is non-fatal, fallback to local storage
        }
        return local;
    }

    public static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return url.Trim().TrimStart('/').Replace('\\', '/').ToLowerInvariant();
    }

    /// <summary>
    /// Ghép bản ghi server với bản ghi local: ưu tiên trùng Id server, SessionId,
    /// cuối cùng là cùng kỹ năng + cùng đề với thời điểm nộp gần nhất (nới 24h để chịu lệch giờ máy).
    /// </summary>
    private static IeltsSubmissionRecord? FindLocalMatch(List<IeltsSubmissionRecord> local, TestSubmissionSyncDto srv)
    {
        if (srv.Id > 0)
        {
            var byId = local.FirstOrDefault(l => l.Id == srv.Id.ToString());
            if (byId != null) return byId;
        }

        if (!string.IsNullOrEmpty(srv.SessionId))
        {
            var bySession = local.FirstOrDefault(l =>
                l.SessionId == srv.SessionId &&
                string.Equals(l.Skill, srv.Skill, StringComparison.OrdinalIgnoreCase));
            if (bySession != null) return bySession;
        }

        return local
            .Where(l => string.Equals(l.Skill, srv.Skill, StringComparison.OrdinalIgnoreCase) && NormalizeUrl(l.ExamUrl) == NormalizeUrl(srv.ExamUrl))
            .OrderBy(l => Math.Abs((l.SubmittedAt - srv.SubmittedAt).TotalMinutes))
            .FirstOrDefault(l => Math.Abs((l.SubmittedAt - srv.SubmittedAt).TotalMinutes) < 24 * 60);
    }

    /// <summary>Lấy N bài làm gần nhất (có điểm) để phân tích trên trang Ưu tiên ôn tập.</summary>
    public async Task<List<SubmissionSummaryDto>> GetMyRecentSubmissionsAsync(int take = 10)
    {
        var all = await GetAllAsync();
        return all
            .Where(s => !string.IsNullOrEmpty(s.Skill) && (s.BandScore.HasValue || s.Score?.Overall > 0 || s.Status == "Graded" || s.Status == "Scored"))
            .OrderByDescending(s => s.SubmittedAt)
            .Take(take)
            .Select(s => new SubmissionSummaryDto
            {
                Skill        = CapitalizeFirst(s.Skill ?? ""),
                BandScore    = s.BandScore ?? s.Score?.Overall ?? 0,
                CorrectCount = s.CorrectCount ?? s.Grading?.CorrectCount ?? 0,
                TotalCount   = s.TotalQuestions ?? s.Grading?.TotalCount ?? 0,
                SubmittedAt  = s.SubmittedAt
            })
            .ToList();
    }

    private static string CapitalizeFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    public async Task<IeltsSubmissionRecord> SaveReadingSubmissionAsync(IeltsSubmissionRecord submission, GradingResult grading)
    {
        submission.Skill = "Reading";
        submission.BandScore = grading.BandScore;
        submission.CorrectCount = grading.CorrectCount;
        submission.TotalQuestions = grading.TotalCount;
        submission.Status = "Scored";
        submission.Grading = new GradingResultRecord
        {
            BandScore = grading.BandScore,
            CorrectCount = grading.CorrectCount,
            TotalCount = grading.TotalCount,
            Questions = grading.Questions.ToDictionary(
                kv => kv.Key,
                kv => new QuestionResultRecord
                {
                    QuestionNumber = kv.Value.QuestionNumber,
                    StudentAnswer = kv.Value.StudentAnswer,
                    CorrectAnswer = kv.Value.CorrectAnswer,
                    AcceptedAnswers = kv.Value.AcceptedAnswers,
                    IsCorrect = kv.Value.IsCorrect,
                    IsBlank = kv.Value.IsBlank
                })
        };

        var profile = await localStorage.GetItemAsync<UserProfile>("user_profile");
        var studentName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : (!string.IsNullOrWhiteSpace(profile?.DisplayName) ? profile.DisplayName : null);
        submission.StudentName = studentName;

        var all = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        var normExam = NormalizeUrl(submission.ExamUrl);
        submission.AttemptNumber = all.Count(s => string.Equals(s.Skill, "Reading", StringComparison.OrdinalIgnoreCase) && NormalizeUrl(s.ExamUrl) == normExam) + 1;

        var details = submission.Grading != null ? System.Text.Json.JsonSerializer.Serialize(submission.Grading) : null;
        var res = await SaveToDbAsync("Reading", submission.ExamUrl, submission.SessionId ?? "", grading.BandScore, grading.CorrectCount, grading.TotalCount, details, "Scored", submission.ExamTitle, studentName, submission.AttemptNumber);
        if (res != null)
        {
            submission.Id = res.Id.ToString();
            submission.R2StorageKey = res.R2StorageKey;
            submission.SubmittedAt = res.SubmittedAt;
        }

        var saved = await SaveAsync(submission);
        return saved;
    }

    public async Task<IeltsSubmissionRecord> SaveListeningSubmissionAsync(IeltsSubmissionRecord submission, GradingResult grading)
    {
        submission.Skill = "Listening";
        submission.BandScore = grading.BandScore;
        submission.CorrectCount = grading.CorrectCount;
        submission.TotalQuestions = grading.TotalCount;
        submission.Status = "Scored";
        submission.Grading = new GradingResultRecord
        {
            BandScore = grading.BandScore,
            CorrectCount = grading.CorrectCount,
            TotalCount = grading.TotalCount,
            Questions = grading.Questions.ToDictionary(
                kv => kv.Key,
                kv => new QuestionResultRecord
                {
                    QuestionNumber = kv.Value.QuestionNumber,
                    StudentAnswer = kv.Value.StudentAnswer,
                    CorrectAnswer = kv.Value.CorrectAnswer,
                    AcceptedAnswers = kv.Value.AcceptedAnswers,
                    IsCorrect = kv.Value.IsCorrect,
                    IsBlank = kv.Value.IsBlank
                })
        };

        var profile = await localStorage.GetItemAsync<UserProfile>("user_profile");
        var studentName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : (!string.IsNullOrWhiteSpace(profile?.DisplayName) ? profile.DisplayName : null);
        submission.StudentName = studentName;

        var all = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        var normExam = NormalizeUrl(submission.ExamUrl);
        submission.AttemptNumber = all.Count(s => string.Equals(s.Skill, "Listening", StringComparison.OrdinalIgnoreCase) && NormalizeUrl(s.ExamUrl) == normExam) + 1;

        var details = submission.Grading != null ? System.Text.Json.JsonSerializer.Serialize(submission.Grading) : null;
        var res = await SaveToDbAsync("Listening", submission.ExamUrl, submission.SessionId ?? "", grading.BandScore, grading.CorrectCount, grading.TotalCount, details, "Scored", submission.ExamTitle, studentName, submission.AttemptNumber);
        if (res != null)
        {
            submission.Id = res.Id.ToString();
            submission.R2StorageKey = res.R2StorageKey;
            submission.SubmittedAt = res.SubmittedAt;
        }

        var saved = await SaveAsync(submission);
        return saved;
    }

    public async Task<IeltsSubmissionRecord?> GetLatestSubmissionForSkillAsync(string skill, string? examUrl, string? sessionId = null)
    {
        var all = await GetAllAsync();
        var normUrl = NormalizeUrl(examUrl);
        return all
            .Where(s => string.Equals(s.Skill, skill, StringComparison.OrdinalIgnoreCase))
            .Where(s => string.IsNullOrEmpty(normUrl) || NormalizeUrl(s.ExamUrl) == normUrl)
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault();
    }

    public async Task<MockTestSummaryModel> GetMockTestSummaryAsync(string? collectionName, string? testTitle, string? sessionId, MockTestDto? mockTest = null)
    {
        var all = await GetAllAsync();
        var summary = new MockTestSummaryModel
        {
            CollectionName = collectionName ?? mockTest?.CollectionName ?? "IELTS Mock Test",
            TestTitle = testTitle ?? mockTest?.Title ?? "Practise Test",
            SessionId = sessionId
        };

        // Match submissions for each skill
        var relevant = all.Where(s => 
            (!string.IsNullOrEmpty(sessionId) && s.SessionId == sessionId) ||
            (!string.IsNullOrEmpty(testTitle) && s.ExamTitle != null && s.ExamTitle.Contains(testTitle, StringComparison.OrdinalIgnoreCase)) ||
            (mockTest != null && (
                s.MockTestId == mockTest.Id ||
                NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.ListeningUrl) ||
                NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.ReadingUrl) ||
                NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.WritingUrl) ||
                NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.SpeakingUrl)))
        ).ToList();

        var listeningSub = relevant
            .Where(s => string.Equals(s.Skill, "listening", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault()
            ?? all
            .Where(s => mockTest != null && !string.IsNullOrEmpty(mockTest.ListeningUrl) && NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.ListeningUrl))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault();

        var readingSub = relevant
            .Where(s => string.Equals(s.Skill, "reading", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault()
            ?? all
            .Where(s => mockTest != null && !string.IsNullOrEmpty(mockTest.ReadingUrl) && NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.ReadingUrl))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault();

        var writingSub = relevant
            .Where(s => string.Equals(s.Skill, "writing", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault()
            ?? all
            .Where(s => mockTest != null && !string.IsNullOrEmpty(mockTest.WritingUrl) && NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.WritingUrl))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault();

        var speakingSub = relevant
            .Where(s => string.Equals(s.Skill, "speaking", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault()
            ?? all
            .Where(s => mockTest != null && !string.IsNullOrEmpty(mockTest.SpeakingUrl) && NormalizeUrl(s.ExamUrl) == NormalizeUrl(mockTest.SpeakingUrl))
            .OrderByDescending(s => s.Status == "Graded" || s.Status == "Scored" ? 1 : 0)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefault();

        if (listeningSub != null)
        {
            summary.Listening = new SkillSummaryItem
            {
                Skill = "Listening",
                IsCompleted = true,
                IsGraded = true,
                Status = listeningSub.Status,
                BandScore = listeningSub.BandScore ?? listeningSub.Score?.Overall ?? 0,
                CorrectCount = listeningSub.CorrectCount ?? listeningSub.Grading?.CorrectCount,
                TotalCount = listeningSub.TotalQuestions ?? listeningSub.Grading?.TotalCount ?? 40,
                DurationSeconds = listeningSub.DurationSeconds,
                SubmittedAt = listeningSub.SubmittedAt,
                ExamUrl = listeningSub.ExamUrl,
                AnswerUrl = listeningSub.AnswerUrl,
                SubmissionId = listeningSub.Id
            };
        }

        if (readingSub != null)
        {
            summary.Reading = new SkillSummaryItem
            {
                Skill = "Reading",
                IsCompleted = true,
                IsGraded = true,
                Status = readingSub.Status,
                BandScore = readingSub.BandScore ?? readingSub.Score?.Overall ?? 0,
                CorrectCount = readingSub.CorrectCount ?? readingSub.Grading?.CorrectCount,
                TotalCount = readingSub.TotalQuestions ?? readingSub.Grading?.TotalCount ?? 40,
                DurationSeconds = readingSub.DurationSeconds,
                SubmittedAt = readingSub.SubmittedAt,
                ExamUrl = readingSub.ExamUrl,
                AnswerUrl = readingSub.AnswerUrl,
                SubmissionId = readingSub.Id
            };
        }

        if (writingSub != null)
        {
            bool isGraded = (writingSub.Status == "Graded" || writingSub.Status == "Scored") && (writingSub.BandScore.HasValue || writingSub.Score?.Overall > 0);
            summary.Writing = new SkillSummaryItem
            {
                Skill = "Writing",
                IsCompleted = true,
                IsGraded = isGraded,
                Status = writingSub.Status ?? "Pending",
                TeacherFeedback = writingSub.TeacherFeedback,
                BandScore = isGraded ? (writingSub.BandScore ?? writingSub.Score?.Overall ?? 0) : 0,
                DurationSeconds = writingSub.DurationSeconds,
                SubmittedAt = writingSub.SubmittedAt,
                ExamUrl = writingSub.ExamUrl,
                SubmissionId = writingSub.Id,
                ScoreReport = writingSub.Score
            };
        }

        if (speakingSub != null)
        {
            bool isGraded = (speakingSub.Status == "Graded" || speakingSub.Status == "Scored") && (speakingSub.BandScore.HasValue || speakingSub.Score?.Overall > 0);
            summary.Speaking = new SkillSummaryItem
            {
                Skill = "Speaking",
                IsCompleted = true,
                IsGraded = isGraded,
                Status = speakingSub.Status ?? "Pending",
                TeacherFeedback = speakingSub.TeacherFeedback,
                BandScore = isGraded ? (speakingSub.BandScore ?? speakingSub.Score?.Overall ?? 0) : 0,
                DurationSeconds = speakingSub.DurationSeconds,
                SubmittedAt = speakingSub.SubmittedAt,
                ExamUrl = speakingSub.ExamUrl,
                SubmissionId = speakingSub.Id,
                ScoreReport = speakingSub.Score
            };
        }

        var gradedScores = new List<double>();
        if (summary.Listening?.IsCompleted == true && summary.Listening.IsGraded && summary.Listening.BandScore >= 0) gradedScores.Add(summary.Listening.BandScore);
        if (summary.Reading?.IsCompleted == true && summary.Reading.IsGraded && summary.Reading.BandScore >= 0) gradedScores.Add(summary.Reading.BandScore);
        if (summary.Writing?.IsCompleted == true && summary.Writing.IsGraded && summary.Writing.BandScore >= 0) gradedScores.Add(summary.Writing.BandScore);
        if (summary.Speaking?.IsCompleted == true && summary.Speaking.IsGraded && summary.Speaking.BandScore >= 0) gradedScores.Add(summary.Speaking.BandScore);

        summary.CompletedSkillsCount = (summary.Listening?.IsCompleted == true ? 1 : 0) +
                                      (summary.Reading?.IsCompleted == true ? 1 : 0) +
                                      (summary.Writing?.IsCompleted == true ? 1 : 0) +
                                      (summary.Speaking?.IsCompleted == true ? 1 : 0);
        summary.OverallBandScore = CalculateOverallBand(gradedScores);
        summary.LastAttemptAt = new[] { 
            summary.Listening?.SubmittedAt, 
            summary.Reading?.SubmittedAt, 
            summary.Writing?.SubmittedAt, 
            summary.Speaking?.SubmittedAt 
        }.Where(d => d.HasValue).OrderByDescending(d => d).FirstOrDefault();

        return summary;
    }

    public static double CalculateOverallBand(IEnumerable<double> scores)
    {
        var valid = scores.Where(s => s >= 0).ToList();
        if (valid.Count == 0) return 0;
        double avg = valid.Average();
        double floor = Math.Floor(avg);
        double frac = avg - floor;
        if (frac >= 0.75) return floor + 1.0;
        if (frac >= 0.25) return floor + 0.5;
        return floor;
    }

    public async Task<IeltsSubmissionRecord> SaveWritingSubmissionAsync(IeltsSubmissionRecord submission)
    {
        submission.Skill = "Writing";
        submission.BandScore = null;
        submission.Score = null;
        submission.Status = "Pending";

        var profile = await localStorage.GetItemAsync<UserProfile>("user_profile");
        var studentName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : (!string.IsNullOrWhiteSpace(profile?.DisplayName) ? profile.DisplayName : null);
        submission.StudentName = studentName;

        var all = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        var normExam = NormalizeUrl(submission.ExamUrl);
        submission.AttemptNumber = all.Count(s => string.Equals(s.Skill, "Writing", StringComparison.OrdinalIgnoreCase) && NormalizeUrl(s.ExamUrl) == normExam) + 1;

        var details = submission.Writing != null ? System.Text.Json.JsonSerializer.Serialize(submission.Writing) : null;
        var res = await SaveToDbAsync("Writing", submission.ExamUrl, submission.SessionId ?? "", 0, 0, 0, details, "Pending", submission.ExamTitle, studentName, submission.AttemptNumber);
        if (res != null)
        {
            submission.Id = res.Id.ToString();
            submission.R2StorageKey = res.R2StorageKey;
            submission.SubmittedAt = res.SubmittedAt;
        }

        var saved = await SaveAsync(submission);
        return saved;
    }

    public async Task<IeltsSubmissionRecord> SaveSpeakingSubmissionAsync(IeltsSubmissionRecord submission)
    {
        submission.Skill = "Speaking";
        submission.BandScore = null;
        submission.Score = null;
        submission.Status = "Pending";

        var profile = await localStorage.GetItemAsync<UserProfile>("user_profile");
        var studentName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : (!string.IsNullOrWhiteSpace(profile?.DisplayName) ? profile.DisplayName : null);
        submission.StudentName = studentName;

        var all = await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();
        var normExam = NormalizeUrl(submission.ExamUrl);
        submission.AttemptNumber = all.Count(s => string.Equals(s.Skill, "Speaking", StringComparison.OrdinalIgnoreCase) && NormalizeUrl(s.ExamUrl) == normExam) + 1;

        var details = submission.Speaking != null ? System.Text.Json.JsonSerializer.Serialize(submission.Speaking) : null;
        var res = await SaveToDbAsync("Speaking", submission.ExamUrl, submission.SessionId ?? "", 0, 0, 0, details, "Pending", submission.ExamTitle, studentName, submission.AttemptNumber);
        if (res != null)
        {
            submission.Id = res.Id.ToString();
            submission.R2StorageKey = res.R2StorageKey;
            submission.SubmittedAt = res.SubmittedAt;
        }

        var saved = await SaveAsync(submission);
        return saved;
    }

    public async Task<IeltsSubmissionRecord> ScoreAndSaveWritingAsync(IeltsSubmissionRecord submission)
    {
        return await SaveWritingSubmissionAsync(submission);
    }

    public async Task<IeltsSubmissionRecord> ScoreAndSaveSpeakingAsync(IeltsSubmissionRecord submission)
    {
        return await SaveSpeakingSubmissionAsync(submission);
    }

    private static IeltsScoreReport ScoreWriting(WritingSubmissionData? writing)
    {
        var tasks = writing?.Tasks ?? new();
        if (tasks.Count == 0)
        {
            return EmptyScore("No writing answer was submitted.");
        }

        var taskScores = tasks.Select(ScoreWritingTask).ToList();
        var weightedTotal = 0d;
        var weightSum = 0d;

        foreach (var item in taskScores)
        {
            var weight = item.TaskNumber == 2 ? 2d : 1d;
            weightedTotal += item.Overall * weight;
            weightSum += weight;
        }

        var overall = RoundHalf(weightedTotal / Math.Max(weightSum, 1));
        return new IeltsScoreReport
        {
            Overall = overall,
            Summary = "Draft local score based on word count, lexical variety, paragraphing, and sentence range. Replace this with the API proxy scorer when it is ready.",
            Criteria =
            [
                new() { Name = "Task Response", Score = RoundHalf(taskScores.Average(x => x.TaskResponse)), Feedback = "Checks whether each task has enough developed content." },
                new() { Name = "Coherence & Cohesion", Score = RoundHalf(taskScores.Average(x => x.Coherence)), Feedback = "Looks at paragraphing and basic linking structure." },
                new() { Name = "Lexical Resource", Score = RoundHalf(taskScores.Average(x => x.Lexical)), Feedback = "Estimates vocabulary range from unique word usage." },
                new() { Name = "Grammar Range", Score = RoundHalf(taskScores.Average(x => x.Grammar)), Feedback = "Estimates sentence range from sentence count and length." }
            ]
        };
    }

    private static IeltsScoreReport ScoreSpeaking(SpeakingSubmissionData? speaking)
    {
        var scoredAnswers = (speaking?.Answers ?? new())
            .Where(answer => answer.Score != null)
            .Select(answer => answer.Score!)
            .ToList();

        if (scoredAnswers.Count == 0)
        {
            return EmptyScore("No speaking answer was recorded.");
        }

        return new IeltsScoreReport
        {
            Overall = RoundHalf(scoredAnswers.Average(x => x.Overall)),
            Summary = $"Saved and scored {scoredAnswers.Count} speaking answer(s). This currently uses the browser rule-based scorer.",
            Criteria =
            [
                new() { Name = "Fluency & Coherence", Score = RoundHalf(scoredAnswers.Average(x => (x.Fluency + x.Coherence) / 2d)), Feedback = "Based on pace, fillers, and simple coherence markers." },
                new() { Name = "Lexical Resource", Score = RoundHalf(scoredAnswers.Average(x => x.Lexical)), Feedback = "Based on vocabulary variety detected in transcripts." },
                new() { Name = "Grammar Range", Score = RoundHalf(scoredAnswers.Average(x => x.Grammar)), Feedback = "Based on simple sentence range heuristics." },
                new() { Name = "Pronunciation", Score = RoundHalf(scoredAnswers.Average(x => x.Fluency)), Feedback = "Temporary proxy until audio-based scoring is connected." }
            ]
        };
    }

    private static WritingTaskScore ScoreWritingTask(WritingTaskSubmission task)
    {
        var answer = task.Answer ?? "";
        var words = answer.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var requiredRatio = task.MinWords <= 0 ? 1d : Math.Min(wordCount / (double)task.MinWords, 1.2);
        var lowerWords = words.Select(w => new string(w.ToLowerInvariant().Where(char.IsLetter).ToArray()))
            .Where(w => w.Length > 2)
            .ToList();
        var uniqueRatio = lowerWords.Count == 0 ? 0 : lowerWords.Distinct().Count() / (double)lowerWords.Count;
        var sentenceCount = answer.Count(ch => ch is '.' or '!' or '?');
        var paragraphCount = answer.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries).Length;

        var taskResponse = Clamp(3.5 + requiredRatio * 3.5 + Math.Min(paragraphCount, 3) * 0.35);
        var coherence = Clamp(4 + Math.Min(paragraphCount, 4) * 0.6 + (wordCount >= task.MinWords ? 0.6 : 0));
        var lexical = Clamp(4 + uniqueRatio * 4);
        var grammar = Clamp(4 + Math.Min(sentenceCount, 12) * 0.22 + (wordCount >= task.MinWords ? 0.8 : 0));
        var overall = RoundHalf((taskResponse + coherence + lexical + grammar) / 4);

        return new WritingTaskScore(task.TaskNumber, taskResponse, coherence, lexical, grammar, overall);
    }

    private static IeltsScoreReport EmptyScore(string message) => new()
    {
        Overall = 0,
        Summary = message,
        Criteria = []
    };

    private static double Clamp(double score) => Math.Max(0, Math.Min(9, score));

    private static double RoundHalf(double score) => Math.Round(score * 2, MidpointRounding.AwayFromZero) / 2d;

    private sealed record WritingTaskScore(
        int TaskNumber,
        double TaskResponse,
        double Coherence,
        double Lexical,
        double Grammar,
        double Overall);
}

public sealed class TestSubmissionDbResponseDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? StudentName { get; set; }
    public string? UserEmail { get; set; }
    public string Skill { get; set; } = "";
    public string ExamUrl { get; set; } = "";
    public string? ExamTitle { get; set; }
    public string? SessionId { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public double BandScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TeacherFeedback { get; set; }
    public string? DetailsJson { get; set; }
    public string? AudioKey { get; set; }
    public string? R2StorageKey { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}
