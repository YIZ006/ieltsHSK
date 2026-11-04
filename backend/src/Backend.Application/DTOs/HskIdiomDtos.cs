namespace Backend.Application.DTOs;

public class ChengyuExampleDto
{
    public string Zh { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
}

public class ChengyuCharDto
{
    public string Char { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string HanViet { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
}

public class WechatWordTokenDto
{
    public string Word { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string MeaningVi { get; set; } = string.Empty;
    public bool IsIdiom { get; set; }
}

public class WechatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Sender { get; set; } = "left";
    public string SpeakerName { get; set; } = string.Empty;
    public string Zh { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string Vi { get; set; } = string.Empty;
    public string? HighlightIdiom { get; set; }
    public List<WechatWordTokenDto> Words { get; set; } = new();
}

public class WechatChatDto
{
    public string ContactName { get; set; } = "珍肺";
    public string RightSpeakerName { get; set; } = "阿明";
    public string Timestamp { get; set; } = "16:32";
    public string AvatarLeft { get; set; } = "/images/idioms/avatar-wet-cat.jpg";
    public string AvatarRight { get; set; } = "/images/idioms/avatar-crying-cat.jpg";
    public List<WechatMessageDto> Messages { get; set; } = new();
}

public class ChengyuDto
{
    public string Id { get; set; } = string.Empty;
    public string Hanzi { get; set; } = string.Empty;
    public string Pinyin { get; set; } = string.Empty;
    public string HanViet { get; set; } = string.Empty;
    public string ShortMeaningVi { get; set; } = string.Empty;
    public string HskLevel { get; set; } = "HSK 5";
    public string Category { get; set; } = "fable_story";
    public string? CategoryVi { get; set; }
    public string MeaningVi { get; set; } = string.Empty;
    public string? OriginStory { get; set; }
    public List<ChengyuCharDto> CharBreakdown { get; set; } = new();
    public List<string> Synonyms { get; set; } = new();
    public List<string> Antonyms { get; set; } = new();
    public List<ChengyuExampleDto> HskExamples { get; set; } = new();
    public WechatChatDto? WechatChat { get; set; }
    public int PopularityScore { get; set; } = 95;
    public List<string> Tags { get; set; } = new();
}

public class SyncChengyuR2Request
{
    public string? JsonContent { get; set; }
}

public class SyncChengyuR2Response
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public string R2Url { get; set; } = string.Empty;
    public bool IsUploaded { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double SizeKb { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
