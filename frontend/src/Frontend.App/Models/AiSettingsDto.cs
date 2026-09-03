namespace Frontend.App.Models;

public class AiProviderConfigDto
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestStatus { get; set; }
    public string? LastTestMessage { get; set; }
    public List<string> AvailableModels { get; set; } = new();
}

public class SystemAiSettingsDto
{
    public AiProviderConfigDto Xkiro { get; set; } = new() { BaseUrl = "https://api.xkiro.com/v1", DefaultModel = "deepseek-v3" };
    public AiProviderConfigDto Gemini { get; set; } = new() { BaseUrl = "https://generativelanguage.googleapis.com/v1beta", DefaultModel = "gemini-2.0-flash" };
    public AiProviderConfigDto OpenAi { get; set; } = new() { BaseUrl = "https://api.openai.com/v1", DefaultModel = "gpt-4o-mini" };
    public AiProviderConfigDto DeepSeek { get; set; } = new() { BaseUrl = "https://api.deepseek.com/v1", DefaultModel = "deepseek-chat" };
    public AiProviderConfigDto Whisper { get; set; } = new() { BaseUrl = "https://api.openai.com/v1", DefaultModel = "whisper-1" };
    public AiProviderConfigDto GroqWhisper { get; set; } = new() { BaseUrl = "https://api.groq.com/openai/v1", DefaultModel = "whisper-large-v3-turbo" };
    
    public string PrimaryWritingProvider { get; set; } = "gemini";
    public string PrimarySpeakingProvider { get; set; } = "gemini";
    public string PrimaryDictionaryProvider { get; set; } = "gemini";
    public string PrimarySttProvider { get; set; } = "browser"; // "browser", "whisper", "groq", "gemini"
}

public class TestAiConnectionRequestDto
{
    public string Provider { get; set; } = "gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string? Model { get; set; }
}

public class TestAiConnectionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string? Detail { get; set; }
    public List<string> AvailableModels { get; set; } = new();
}
