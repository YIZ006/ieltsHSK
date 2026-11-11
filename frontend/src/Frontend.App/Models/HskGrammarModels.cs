namespace Frontend.App.Models;

public class HskGrammarModel
{
    public Dictionary<string, HskGrammarLevelData> Levels { get; set; } = new();
}

public class HskGrammarLevelData
{
    public List<HskGrammarGroup> Groups { get; set; } = new();
    public List<HskGrammarTopicItem>? Topics { get; set; }
    public List<HskGrammarTaskItem>? Tasks { get; set; }
}

public class HskGrammarGroup
{
    public string Name { get; set; } = string.Empty;
    public List<HskGrammarCategory> Loais { get; set; } = new();
}

public class HskGrammarCategory
{
    public string Name { get; set; } = string.Empty;
    public List<HskGrammarPointItem> Items { get; set; } = new();
}

public class HskGrammarPointItem
{
    public string Id { get; set; } = string.Empty;
    public string Sub { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<HskGrammarExample>? Examples { get; set; }
}

public class HskGrammarExample
{
    public string Zh { get; set; } = string.Empty;
    public string Py { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
}

public class HskGrammarTopicItem
{
    public string L1 { get; set; } = string.Empty;
    public string L2 { get; set; } = string.Empty;
    public string L3 { get; set; } = string.Empty;
}

public class HskGrammarTaskItem
{
    public string No { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = new();
}
