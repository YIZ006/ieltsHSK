using Blazored.LocalStorage;
using Frontend.App.Models;

namespace Frontend.App.Services;

public sealed class ExamSubmissionService(ILocalStorageService localStorage)
{
    private const string StorageKey = "ielts-exam-submissions";

    public async Task<IeltsSubmissionRecord> SaveAsync(IeltsSubmissionRecord submission)
    {
        submission.Id = string.IsNullOrWhiteSpace(submission.Id) ? Guid.NewGuid().ToString("N") : submission.Id;
        submission.SubmittedAt = submission.SubmittedAt == default ? DateTimeOffset.UtcNow : submission.SubmittedAt;

        var submissions = await GetAllAsync();
        var index = submissions.FindIndex(item => item.Id == submission.Id);
        if (index >= 0) submissions[index] = submission;
        else submissions.Insert(0, submission);

        await localStorage.SetItemAsync(StorageKey, submissions);
        return submission;
    }

    public async Task<List<IeltsSubmissionRecord>> GetAllAsync()
        => await localStorage.GetItemAsync<List<IeltsSubmissionRecord>>(StorageKey) ?? new();

    public async Task<IeltsSubmissionRecord> ScoreAndSaveWritingAsync(IeltsSubmissionRecord submission)
    {
        submission.Score = ScoreWriting(submission.Writing);
        submission.Status = "Scored";
        return await SaveAsync(submission);
    }

    public async Task<IeltsSubmissionRecord> ScoreAndSaveSpeakingAsync(IeltsSubmissionRecord submission)
    {
        submission.Score = ScoreSpeaking(submission.Speaking);
        submission.Status = "Scored";
        return await SaveAsync(submission);
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
