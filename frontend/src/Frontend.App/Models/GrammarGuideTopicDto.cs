namespace Frontend.App.Models;

public class GrammarGuideSectionDto
{
    public string Key { get; set; } = string.Empty;
    public string Overline { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-journal-bookmark";
    public string ColorTheme { get; set; } = "#3b82f6";
    public List<GrammarGuideTopicDto> Topics { get; set; } = new();
}

public class GrammarGuideTopicDto
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EnglishTitle { get; set; } = string.Empty;
    public string SectionKey { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-clock-history";
    public string IconBgColor { get; set; } = "#2563eb";
    public string DifficultyLevel { get; set; } = "Cơ bản"; // Cơ bản, Trung cấp, Nâng cao
    public int ReadTimeMinutes { get; set; } = 5;
    public int OrderIndex { get; set; } = 1;
    public string? FormulaPreview { get; set; }
    public string? SkillTarget { get; set; } // Writing Task 1, Task 2, Speaking, Foundation
    public List<string> Tags { get; set; } = new();

    // Lesson Details
    public string ConceptExplanation { get; set; } = string.Empty;
    public List<GrammarFormulaBlock> Formulas { get; set; } = new();
    public List<GrammarRuleTableItem> RuleTables { get; set; } = new();
    public List<GrammarUsageItem> Usages { get; set; } = new();
    public List<string> SignalWords { get; set; } = new();
    public string? SignalWordPlacementRule { get; set; }
    public List<GrammarPitfallItem> CommonMistakes { get; set; } = new();
    public List<GrammarExerciseItem> Exercises { get; set; } = new();
    public string? LearningTip { get; set; }
    public List<string>? RelatedBandStructureCodes { get; set; }
    public List<IrregularWordItem>? IrregularList { get; set; }
}

public class GrammarFormulaBlock
{
    public string Type { get; set; } = string.Empty; // Khẳng định (+), Phủ định (-), Nghi vấn (?)
    public string Formula { get; set; } = string.Empty;
    public string ColorVariant { get; set; } = "blue"; // blue, red, green, purple
    public List<string> Breakdown { get; set; } = new();
}

public class GrammarRuleTableItem
{
    public string Rule { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class GrammarUsageItem
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<GrammarBilingualExample> Examples { get; set; } = new();
}

public class GrammarBilingualExample
{
    public string English { get; set; } = string.Empty;
    public string Vietnamese { get; set; } = string.Empty;
    public string? HighlightText { get; set; }
}

public class GrammarPitfallItem
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WrongExample { get; set; } = string.Empty;
    public string CorrectExample { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class GrammarExerciseItem
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; } = 0;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ExerciseType { get; set; } = "multiple_choice"; // multiple_choice, fill_blank
}

public class IrregularWordItem
{
    public string Base { get; set; } = string.Empty;
    public string PastSimple { get; set; } = string.Empty;
    public string PastParticiple { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? Pronunciation { get; set; }
    public string? Category { get; set; } // verbs, nouns, comparisons
}
