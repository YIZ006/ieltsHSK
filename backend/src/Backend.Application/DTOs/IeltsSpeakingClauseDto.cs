using System.Collections.Generic;

namespace Backend.Application.DTOs;

public class SpeakingClauseFeedbackRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public int PartNumber { get; set; } = 1;
}

public class SpeakingClauseItemDto
{
    public string Text { get; set; } = string.Empty;
    public string Status { get; set; } = "ok"; // "bad", "mid", "ok"
    public string Feedback { get; set; } = string.Empty;
    public List<string> NativeAlternatives { get; set; } = new();
}

public class SpeakingClauseFeedbackResponse
{
    public List<SpeakingClauseItemDto> Clauses { get; set; } = new();
    public string OverallAssessment { get; set; } = string.Empty;
    public List<string> ModelAnswers { get; set; } = new();
}
