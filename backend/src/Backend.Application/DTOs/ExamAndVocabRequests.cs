namespace Backend.Application.DTOs;

public record CreateExamRequest(string Title, string DataUrl, string Category = "IELTS");
public record SaveToeicExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskSaveExamRequest(string CollectionName, string Title, int? MockTestId, string ExamData);
public record HskVocabularyRequest(string HskLevel, string Hanzi, string Pinyin, string Meaning, string? WordType, string? ExampleSentence, string? ExamplePinyin, string? ExampleMeaning, string? AudioUrl, int? DisplayOrder, bool? IsActive);
public record UpdateVocabProgressRequest(bool Learned);
public record MigrateVocabProgressRequest(List<int> VocabularyIds);
public record IeltsVocabularyRequest(string Word, string? Phonetic, string? PartOfSpeech, string Meaning, string? Example, string? ExampleMeaning, string? Topic, string? CefrLevel, int? DisplayOrder, bool? IsActive);
