namespace Backend.Application.DTOs;

public record AddStudyTimeRequest(int Seconds, DateTime? Date = null);

public record DailyStudyItemDto(DateTime Date, int Seconds);

public record StudyActivityResponseDto(
    int CurrentStreak,
    int BestStreak,
    int TodaySeconds,
    List<DailyStudyItemDto> DailyActivities,
    List<string> ActiveDays
);

public record MigrateStudyActivityRequest(
    List<string>? ActiveDays = null,
    Dictionary<string, int>? DailySeconds = null
);
