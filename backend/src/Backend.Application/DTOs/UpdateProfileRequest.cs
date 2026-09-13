namespace Backend.Application.DTOs;

public record UpdateProfileRequest(
    string? FullName = null,
    string? Username = null,
    string? Avatar = null,
    string? AvatarColor = null,
    string? Bio = null,
    string? TargetExam = null,
    string? TargetScore = null,
    DateTime? TargetDeadline = null,
    string? IeltsLevel = null,
    string? HskLevel = null,
    string? Level = null
);
