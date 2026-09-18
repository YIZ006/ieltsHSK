using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static List<GrammarGuideSectionDto>? _sections;

    public static List<GrammarGuideSectionDto> GetSections()
    {
        if (_sections != null) return _sections;

        _sections = new List<GrammarGuideSectionDto>
        {
            BuildTensesSection(),
            BuildSentenceStructureSection(),
            BuildPartsOfSpeechSection(),
            BuildAdvancedTopicsSection(),
            BuildIrregularSection(),
            BuildTipsSection()
        };

        return _sections;
    }

    public static List<GrammarGuideTopicDto> GetAllTopics()
    {
        return GetSections().SelectMany(s => s.Topics).ToList();
    }

    public static GrammarGuideTopicDto? GetTopicBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var cleanSlug = slug.Trim().ToLowerInvariant();
        return GetAllTopics().FirstOrDefault(t => t.Slug.ToLowerInvariant() == cleanSlug);
    }

    public static (GrammarGuideTopicDto? Prev, GrammarGuideTopicDto? Next) GetAdjacentTopics(string slug)
    {
        var all = GetAllTopics();
        var index = all.FindIndex(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (index == -1) return (null, null);

        var prev = index > 0 ? all[index - 1] : null;
        var next = index < all.Count - 1 ? all[index + 1] : null;
        return (prev, next);
    }
}
