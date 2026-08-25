namespace Backend.Application.DTOs;

public class SaveSpeakAlongRequest
{
    public string Part { get; set; } = "100Sentences";
    public object Data { get; set; } = new();
}
