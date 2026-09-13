namespace Backend.Domain.Entities;

public class UserNotificationRead
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int NotificationId { get; set; }
    public Notification? Notification { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
