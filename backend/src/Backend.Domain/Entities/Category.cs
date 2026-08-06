namespace Backend.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., 'Luyện thi HSK', 'Luyện nghe tiếng Anh'
    public string? Description { get; set; }

    // Navigation Property
    public ICollection<Website> Websites { get; set; } = new List<Website>();
}
