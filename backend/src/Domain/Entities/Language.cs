namespace Backend.Domain.Entities;

public class Language
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., 'Tiếng Anh', 'Tiếng Trung'
    public string Code { get; set; } = string.Empty; // e.g., 'EN', 'ZH'

    // Navigation Property
    public ICollection<Website> Websites { get; set; } = new List<Website>();
}
