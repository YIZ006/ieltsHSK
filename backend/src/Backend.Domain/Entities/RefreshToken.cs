namespace Backend.Domain.Entities;

/// <summary>
/// Refresh token dùng để gia hạn phiên đăng nhập (ghi nhớ tài khoản) mà không cần đăng nhập lại.
/// Mỗi lần refresh sẽ xoay vòng (rotate): token cũ bị thu hồi và cấp token mới.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Giá trị token ngẫu nhiên (base64 64 bytes) — được lưu và so khớp trực tiếp
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
