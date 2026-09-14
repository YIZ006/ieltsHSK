namespace Backend.Domain.Entities;

public enum FriendshipStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Blocked = 3
}

public class Friendship
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int FriendId { get; set; }
    public User Friend { get; set; } = null!;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    /// <summary>ID người dùng thực hiện hành động cuối cùng (ví dụ: người gửi lời mời, người từ chối)</summary>
    public int ActionUserId { get; set; }

    /// <summary>Đánh dấu bạn thân / yêu thích (ghim lên đầu danh sách như Wolvesville)</summary>
    public bool IsFavorite { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
