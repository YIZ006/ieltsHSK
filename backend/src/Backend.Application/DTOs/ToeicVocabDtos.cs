namespace Backend.Application.DTOs;

public record ToeicVocabDto(
    int Id,
    string Word,
    string Ipa,
    string Meaning,
    string? Example,
    string Topic,
    bool IsCustom
);

public record CreateToeicVocabRequest(
    string Word,
    string? Ipa,
    string Meaning,
    string? Example,
    string? Topic
);

public record UpdateToeicVocabProgressRequest(
    int VocabularyId,
    string Status // "Learned", "Again", "None"
);

public record ToeicVocabProgressResponse(
    List<int> LearnedIds,
    List<int> AgainIds
);

public record MigrateToeicVocabRequest(
    List<int>? LearnedIds = null,
    List<int>? AgainIds = null,
    List<CreateToeicVocabRequest>? CustomWords = null
);
