namespace Frontend.App.Models;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system"; // system, exam, update, reminder, reward
    public string Icon { get; set; } = "bi-bell-fill";
    public string? TargetUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class AdminNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system";
    public string Icon { get; set; } = "bi-bell-fill";
    public string? TargetUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByAdmin { get; set; }
    public bool IsBroadcast { get; set; } = true;
    public int? UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public int ReadCount { get; set; }
}

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system";
    public string Icon { get; set; } = "bi-bell-fill";
    public string? TargetUrl { get; set; }
    public bool IsBroadcast { get; set; } = true;
    public int? UserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system";
    public string Icon { get; set; } = "bi-bell-fill";
    public string? TargetUrl { get; set; }
    public bool IsBroadcast { get; set; } = true;
    public int? UserId { get; set; }
    public bool IsActive { get; set; } = true;
}
