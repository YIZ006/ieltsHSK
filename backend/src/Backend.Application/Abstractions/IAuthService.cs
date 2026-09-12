using Backend.Application.DTOs;

namespace Backend.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> AdminLoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Đổi refresh token lấy cặp access token + refresh token mới (rotation).</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    /// <summary>Thu hồi refresh token khi người dùng đăng xuất.</summary>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
