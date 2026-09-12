namespace Backend.Domain.Entities;

public class UserStudyActivity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    // Ngày học (phần ngày Date theo UTC)
    public DateTime ActivityDate { get; set; }
    
    // Tổng số giây học trong ngày
    public int StudySeconds { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
