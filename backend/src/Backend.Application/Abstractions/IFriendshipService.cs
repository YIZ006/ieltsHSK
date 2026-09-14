using Backend.Application.DTOs;

namespace Backend.Application.Abstractions;

public interface IFriendshipService
{
    Task<FriendsSummaryDto> GetSummaryAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<List<FriendDto>> GetFriendsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<List<UserSearchResultDto>> SearchUsersAsync(int currentUserId, string query, CancellationToken cancellationToken = default);
    Task<bool> SendFriendRequestAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    Task<bool> AcceptFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default);
    Task<bool> DeclineFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default);
    Task<bool> CancelFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFriendAsync(int currentUserId, int friendId, CancellationToken cancellationToken = default);
    Task<bool> ToggleFavoriteAsync(int currentUserId, int friendId, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(int currentUserId, CancellationToken cancellationToken = default);
}
