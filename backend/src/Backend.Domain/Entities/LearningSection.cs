namespace Backend.Domain.Entities;

public class LearningSection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Language { get; set; } = "IELTS";
    public int OrderIndex { get; set; }
}
