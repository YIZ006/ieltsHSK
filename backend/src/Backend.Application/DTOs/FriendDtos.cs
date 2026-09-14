namespace Backend.Application.DTOs;

public class FriendDto
{
    public int Id { get; set; }
    public int FriendshipId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? Bio { get; set; }
    public string? TargetExam { get; set; }
    public string? TargetScore { get; set; }
    public string? Level { get; set; }
    public int Streak { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastActive { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime FriendSince { get; set; }
}

public class FriendRequestDto
{
    public int FriendshipId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? TargetExam { get; set; }
    public string? TargetScore { get; set; }
    public int Streak { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsIncoming { get; set; }
}

public class UserSearchResultDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? AvatarColor { get; set; }
    public string? TargetExam { get; set; }
    public string? TargetScore { get; set; }
    public string? Level { get; set; }
    public int Streak { get; set; }
    public bool IsOnline { get; set; }
    public string Relationship { get; set; } = "None"; // "None", "PendingIncoming", "PendingOutgoing", "Friends"
    public int? FriendshipId { get; set; }
}

public class FriendsSummaryDto
{
    public int TotalFriends { get; set; }
    public int OnlineFriends { get; set; }
    public int PendingIncomingCount { get; set; }
    public List<FriendDto> Friends { get; set; } = new();
    public List<FriendRequestDto> IncomingRequests { get; set; } = new();
    public List<FriendRequestDto> OutgoingRequests { get; set; } = new();
}
