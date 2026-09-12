namespace Backend.Application.DTOs;

public record SaveCheckpointRequest(
    string UserIdentifier,
    string Skill,
    string ExamUrl,
    int? MockTestId,
    string CheckpointDataJson,
    int SecondsRemaining
);

public record ExamCheckpointDto(
    int Id,
    string UserIdentifier,
    string Skill,
    string ExamUrl,
    int? MockTestId,
    string CheckpointDataJson,
    int SecondsRemaining,
    DateTime LastSavedAt
);
