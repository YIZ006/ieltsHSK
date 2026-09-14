using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Services;

public class FriendshipService(AppDbContext dbContext) : IFriendshipService
{
    private static bool IsUserOnline(DateTime? lastActive) =>
        lastActive.HasValue && (DateTime.UtcNow - lastActive.Value).TotalMinutes <= 5;

    public async Task HeartbeatAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FindAsync(new object[] { currentUserId }, cancellationToken);
        if (user != null)
        {
            user.LastActive = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<FriendDto>> GetFriendsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var friendships = await dbContext.Friendships
            .AsNoTracking()
            .Include(f => f.User)
            .Include(f => f.Friend)
            .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId) && f.Status == FriendshipStatus.Accepted)
            .ToListAsync(cancellationToken);

        var list = new List<FriendDto>();
        foreach (var f in friendships)
        {
            var other = f.UserId == currentUserId ? f.Friend : f.User;
            if (other == null || !other.IsActive) continue;

            list.Add(new FriendDto
            {
                Id = other.Id,
                FriendshipId = f.Id,
                Username = other.Username,
                FullName = other.FullName,
                Avatar = other.Avatar,
                AvatarColor = other.AvatarColor,
                Bio = other.Bio,
                TargetExam = other.TargetExam,
                TargetScore = other.TargetScore,
                Level = other.Level,
                Streak = other.Streak,
                IsOnline = IsUserOnline(other.LastActive),
                LastActive = other.LastActive,
                IsFavorite = f.IsFavorite,
                FriendSince = f.UpdatedAt ?? f.CreatedAt
            });
        }

        // Sắp xếp: Yêu thích -> Đang Online -> Tên
        return list
            .OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.IsOnline)
            .ThenBy(x => x.Username)
            .ToList();
    }

    public async Task<FriendsSummaryDto> GetSummaryAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var friends = await GetFriendsAsync(currentUserId, cancellationToken);

        // Lời mời nhận được (incoming)
        var incomingEntities = await dbContext.Friendships
            .AsNoTracking()
            .Include(f => f.User)
            .Where(f => f.FriendId == currentUserId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var incoming = incomingEntities.Select(f => new FriendRequestDto
        {
            FriendshipId = f.Id,
            UserId = f.User.Id,
            Username = f.User.Username,
            FullName = f.User.FullName,
            Avatar = f.User.Avatar,
            AvatarColor = f.User.AvatarColor,
            TargetExam = f.User.TargetExam,
            TargetScore = f.User.TargetScore,
            Streak = f.User.Streak,
            CreatedAt = f.CreatedAt,
            IsIncoming = true
        }).ToList();

        // Lời mời đã gửi (outgoing)
        var outgoingEntities = await dbContext.Friendships
            .AsNoTracking()
            .Include(f => f.Friend)
            .Where(f => f.UserId == currentUserId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var outgoing = outgoingEntities.Select(f => new FriendRequestDto
        {
            FriendshipId = f.Id,
            UserId = f.Friend.Id,
            Username = f.Friend.Username,
            FullName = f.Friend.FullName,
            Avatar = f.Friend.Avatar,
            AvatarColor = f.Friend.AvatarColor,
            TargetExam = f.Friend.TargetExam,
            TargetScore = f.Friend.TargetScore,
            Streak = f.Friend.Streak,
            CreatedAt = f.CreatedAt,
            IsIncoming = false
        }).ToList();

        return new FriendsSummaryDto
        {
            TotalFriends = friends.Count,
            OnlineFriends = friends.Count(x => x.IsOnline),
            PendingIncomingCount = incoming.Count,
            Friends = friends,
            IncomingRequests = incoming,
            OutgoingRequests = outgoing
        };
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(int currentUserId, string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<UserSearchResultDto>();

        var clean = query.Trim().TrimStart('@').ToLowerInvariant();
        if (clean.Length < 1) return new List<UserSearchResultDto>();

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && 
                        ((u.Username != null && EF.Functions.ILike(u.Username, $"%{clean}%")) || 
                         (u.FullName != null && EF.Functions.ILike(u.FullName, $"%{clean}%"))))
            .OrderByDescending(u => u.Username.ToLower() == clean)
            .ThenByDescending(u => u.Username.ToLower().StartsWith(clean))
            .ThenBy(u => u.Username)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (users.Count == 0) return new List<UserSearchResultDto>();

        var userIds = users.Select(u => u.Id).ToList();

        var existingFriendships = await dbContext.Friendships
            .AsNoTracking()
            .Where(f => (f.UserId == currentUserId && userIds.Contains(f.FriendId)) ||
                        (f.FriendId == currentUserId && userIds.Contains(f.UserId)))
            .ToListAsync(cancellationToken);

        var results = new List<UserSearchResultDto>();
        foreach (var u in users)
        {
            var rel = existingFriendships.FirstOrDefault(f => 
                (f.UserId == currentUserId && f.FriendId == u.Id) ||
                (f.FriendId == currentUserId && f.UserId == u.Id));

            string status = "None";
            int? friendshipId = null;

            if (u.Id == currentUserId)
            {
                status = "Self";
            }
            else if (rel != null)
            {
                friendshipId = rel.Id;
                if (rel.Status == FriendshipStatus.Accepted)
                {
                    status = "Friends";
                }
                else if (rel.Status == FriendshipStatus.Pending)
                {
                    status = rel.UserId == currentUserId ? "PendingOutgoing" : "PendingIncoming";
                }
            }

            results.Add(new UserSearchResultDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Avatar = u.Avatar,
                AvatarColor = u.AvatarColor,
                TargetExam = u.TargetExam,
                TargetScore = u.TargetScore,
                Level = u.Level,
                Streak = u.Streak,
                IsOnline = IsUserOnline(u.LastActive),
                Relationship = status,
                FriendshipId = friendshipId
            });
        }

        // Ưu tiên kết quả khớp chính xác username trước
        return results
            .OrderByDescending(x => x.Username.ToLower() == clean)
            .ThenByDescending(x => x.Username.ToLower().StartsWith(clean))
            .ThenBy(x => x.Username)
            .ToList();
    }

    public async Task<bool> SendFriendRequestAsync(int currentUserId, int targetUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == targetUserId) return false;

        var targetUser = await dbContext.Users.FindAsync(new object[] { targetUserId }, cancellationToken);
        if (targetUser == null || !targetUser.IsActive) return false;

        var existing = await dbContext.Friendships
            .FirstOrDefaultAsync(f => (f.UserId == currentUserId && f.FriendId == targetUserId) ||
                                      (f.UserId == targetUserId && f.FriendId == currentUserId),
                                 cancellationToken);

        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted) return true;

            // Nếu người kia đã gửi lời mời trước -> Tự động kết bạn luôn!
            if (existing.Status == FriendshipStatus.Pending && existing.FriendId == currentUserId)
            {
                existing.Status = FriendshipStatus.Accepted;
                existing.ActionUserId = currentUserId;
                existing.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            // Nếu mình đã gửi rồi
            if (existing.Status == FriendshipStatus.Pending && existing.UserId == currentUserId)
            {
                return true;
            }

            // Nếu từng bị từ chối -> Re-send
            existing.UserId = currentUserId;
            existing.FriendId = targetUserId;
            existing.Status = FriendshipStatus.Pending;
            existing.ActionUserId = currentUserId;
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var friendship = new Friendship
        {
            UserId = currentUserId,
            FriendId = targetUserId,
            Status = FriendshipStatus.Pending,
            ActionUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Friendships.Add(friendship);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AcceptFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default)
    {
        var f = await dbContext.Friendships.FindAsync(new object[] { friendshipId }, cancellationToken);
        if (f == null || f.FriendId != currentUserId || f.Status != FriendshipStatus.Pending)
        {
            return false;
        }

        f.Status = FriendshipStatus.Accepted;
        f.ActionUserId = currentUserId;
        f.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeclineFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default)
    {
        var f = await dbContext.Friendships.FindAsync(new object[] { friendshipId }, cancellationToken);
        if (f == null || f.FriendId != currentUserId || f.Status != FriendshipStatus.Pending)
        {
            return false;
        }

        dbContext.Friendships.Remove(f);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelFriendRequestAsync(int currentUserId, int friendshipId, CancellationToken cancellationToken = default)
    {
        var f = await dbContext.Friendships.FindAsync(new object[] { friendshipId }, cancellationToken);
        if (f == null || f.UserId != currentUserId || f.Status != FriendshipStatus.Pending)
        {
            return false;
        }

        dbContext.Friendships.Remove(f);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveFriendAsync(int currentUserId, int friendId, CancellationToken cancellationToken = default)
    {
        var f = await dbContext.Friendships
            .FirstOrDefaultAsync(f => (f.UserId == currentUserId && f.FriendId == friendId) ||
                                      (f.UserId == friendId && f.FriendId == currentUserId),
                                 cancellationToken);

        if (f != null && f.Status == FriendshipStatus.Accepted)
        {
            dbContext.Friendships.Remove(f);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<bool> ToggleFavoriteAsync(int currentUserId, int friendId, CancellationToken cancellationToken = default)
    {
        var f = await dbContext.Friendships
            .FirstOrDefaultAsync(f => (f.UserId == currentUserId && f.FriendId == friendId) ||
                                      (f.UserId == friendId && f.FriendId == currentUserId),
                                 cancellationToken);

        if (f != null && f.Status == FriendshipStatus.Accepted)
        {
            f.IsFavorite = !f.IsFavorite;
            f.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }
}
