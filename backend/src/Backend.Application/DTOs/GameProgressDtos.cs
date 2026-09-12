namespace Backend.Application.DTOs;

public record UserGameProgressDto(
    string GameType,
    string Level,
    int CurrentStage,
    int MaxUnlockedStage,
    int HighScore,
    DateTime UpdatedAt
);

public record SaveGameProgressRequest(
    string GameType,
    string Level,
    int? CurrentStage = null,
    int? MaxUnlockedStage = null,
    int? Score = null
);

public record MigrateGameProgressRequest(
    List<SaveGameProgressRequest> ProgressItems
);
