namespace Backend.Application.DTOs;

public class LearningSectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}
