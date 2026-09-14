using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Backend.Application.Abstractions;
using Backend.Application.Common;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Infrastructure.Services;

public class AuthService(AppDbContext dbContext, IConfiguration configuration, IMemoryCache cache) : IAuthService
{
    // Access token (JWT) sống ngắn (30 phút cho học viên, 12 giờ cho quản trị viên), phiên dài hạn được bảo đảm bởi refresh token
    private static readonly TimeSpan AccessTokenDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AdminTokenDuration = TimeSpan.FromHours(12);
    private const int RefreshTokenDays = 30;

    // Cho phép tái sử dụng refresh token đã xoay vòng trong khoảng thời gian ngắn (10s)
    // để các tab/cấu hình trình duyệt refresh gần như đồng thời không bị văng phiên
    private const double ReuseGraceSeconds = 10;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Chống tiêm mã độc (XSS / Script Injection) trong các ô nhập
        SecuritySanitizer.ValidateSafeText(request.FullName, "Họ và tên");
        SecuritySanitizer.ValidateSafeText(request.Username, "Tên đăng nhập");
        SecuritySanitizer.ValidateSafeText(request.Email, "Email");
        SecuritySanitizer.ValidateSafeText(request.Password, "Mật khẩu");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 ||
            !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
        {
            throw new Exception("Mật khẩu phải có tối thiểu 8 ký tự, bao gồm cả chữ cái và chữ số.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new Exception("Email này đã được sử dụng. Vui lòng dùng email khác hoặc đăng nhập.");
        }

        string finalUsername;
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var cleanUsername = request.Username.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanUsername, @"^[a-zA-Z0-9]+$"))
            {
                throw new Exception("Tên đăng nhập chỉ được bao gồm chữ cái và chữ số (không chứa ký tự đặc biệt hay khoảng trắng).");
            }
            if (await dbContext.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername.ToLower(), cancellationToken))
            {
                throw new Exception("Tên đăng nhập này đã tồn tại. Vui lòng chọn tên đăng nhập khác.");
            }
            finalUsername = cleanUsername;
        }
        else
        {
            // Auto-generate a unique username from email
            var baseUsername = normalizedEmail.Split('@')[0];
            var username = baseUsername;
            var suffix = 1;
            while (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
            {
                username = $"{baseUsername}{suffix++}";
            }
            finalUsername = username;
        }

        var user = new User
        {
            Username = finalUsername,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? finalUsername : request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        var clean = username.Trim().ToLowerInvariant();
        return await dbContext.Users.AnyAsync(u => u.Username.ToLower() == clean, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Chống tiêm mã độc vào ô đăng nhập
        SecuritySanitizer.ValidateSafeText(request.UsernameOrEmail, "Tên đăng nhập hoặc Email");
        SecuritySanitizer.ValidateSafeText(request.Email, "Email");

        var input = request.ResolvedUsernameOrEmail.Trim().ToLowerInvariant();
        var lockKey = $"lockout_user_{input}";
        if (cache.TryGetValue(lockKey, out _))
        {
            throw new Exception("Tài khoản tạm thời bị khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau 15 phút.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Email.ToLower() == input || u.Username.ToLower() == input, 
            cancellationToken);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            var failKey = $"login_fail_user_{input}";
            var count = (cache.Get<int?>(failKey) ?? 0) + 1;
            cache.Set(failKey, count, TimeSpan.FromMinutes(15));
            if (count >= 5)
            {
                cache.Set(lockKey, true, TimeSpan.FromMinutes(15));
                cache.Remove(failKey);
                throw new Exception("Bạn đã nhập sai mật khẩu 5 lần. Tài khoản tạm thời bị khóa trong 15 phút.");
            }
            throw new Exception("Tên đăng nhập / Email hoặc mật khẩu không đúng.");
        }

        // Đăng nhập thành công -> xóa bộ đếm thất bại
        cache.Remove($"login_fail_user_{input}");
        cache.Remove(lockKey);

        if (!user.IsActive)
        {
            throw new Exception("Tài khoản bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        
        dbContext.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = user.Id,
            Action = "login",
            Detail = "Normal login"
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> AdminLoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Chống tiêm mã độc vào ô đăng nhập quản trị
        SecuritySanitizer.ValidateSafeText(request.UsernameOrEmail, "Tên đăng nhập hoặc Email");
        SecuritySanitizer.ValidateSafeText(request.Email, "Email");

        var input = request.ResolvedUsernameOrEmail.Trim().ToLowerInvariant();
        var lockKey = $"lockout_admin_{input}";
        if (cache.TryGetValue(lockKey, out _))
        {
            throw new Exception("Tài khoản Quản trị viên tạm thời bị khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau 15 phút.");
        }

        var admin = await dbContext.Admins.SingleOrDefaultAsync(
            a => a.Email.ToLower() == input || a.Username.ToLower() == input, 
            cancellationToken);
        
        if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
        {
            var failKey = $"login_fail_admin_{input}";
            var count = (cache.Get<int?>(failKey) ?? 0) + 1;
            cache.Set(failKey, count, TimeSpan.FromMinutes(15));
            if (count >= 5)
            {
                cache.Set(lockKey, true, TimeSpan.FromMinutes(15));
                cache.Remove(failKey);
                throw new Exception("Bạn đã nhập sai mật khẩu Quản trị viên 5 lần. Tài khoản tạm thời bị khóa trong 15 phút.");
            }
            throw new Exception("Tên đăng nhập / Email hoặc mật khẩu Quản trị viên không đúng.");
        }

        // Đăng nhập thành công -> xóa bộ đếm thất bại
        cache.Remove($"login_fail_admin_{input}");
        cache.Remove(lockKey);

        if (!admin.IsActive)
        {
            throw new Exception("Tài khoản Quản trị viên đã bị vô hiệu hóa.");
        }

        admin.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateAdminJwtToken(admin, AdminTokenDuration);
        return new AuthResponse(token, admin.FullName, admin.Email);
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        var expectedClientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(expectedClientId) || expectedClientId == "YOUR_GOOGLE_CLIENT_ID")
        {
            expectedClientId = "161879464750-2ssdrim1ltgg2nh7nr5agvrbk52bevih.apps.googleusercontent.com";
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { expectedClientId }
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new Exception("Invalid Google token.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        
        // Nếu user chưa từng đăng ký, tự động tạo tài khoản với thông tin thực từ Google
        if (user == null)
        {
            var baseUsername = normalizedEmail.Split('@')[0];
            var username = baseUsername;
            var suffix = 1;
            while (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
            {
                username = $"{baseUsername}{suffix++}";
            }

            user = new User
            {
                Username = username,
                FullName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name.Trim() : username,
                Email = normalizedEmail,
                Avatar = !string.IsNullOrWhiteSpace(payload.Picture) ? payload.Picture : null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = user.Id,
                Action = "register_google",
                Detail = "Auto-registered via Google OAuth"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (!user.IsActive)
            {
                throw new Exception("Account is disabled. Please contact admin.");
            }

            // Tự động đồng bộ tên nếu trong database đang là rỗng hoặc "Học viên"
            if ((string.IsNullOrWhiteSpace(user.FullName) || user.FullName == "Học viên" || user.FullName == user.Username) && !string.IsNullOrWhiteSpace(payload.Name))
            {
                user.FullName = payload.Name.Trim();
            }

            user.LastLoginAt = DateTime.UtcNow;
            dbContext.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = user.Id,
                Action = "login_google",
                Detail = "Google login"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsActive)
        {
            throw new Exception("Account is disabled. Please contact admin.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        
        dbContext.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = user.Id,
            Action = "login_google",
            Detail = "Google login"
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RegisterWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { configuration["Google:ClientId"] }
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new Exception("Invalid Google token.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        // Nếu email đã tồn tại → chuyển qua login
        var existingUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (existingUser != null)
        {
            return await LoginWithGoogleAsync(request, cancellationToken);
        }

        // Tạo username duy nhất từ email
        var baseUsername = normalizedEmail.Split('@')[0];
        var username = baseUsername;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{baseUsername}{suffix++}";
        }

        // Tạo tài khoản mới từ thông tin Google (không có password thực, user sẽ dùng Google để đăng nhập)
        var user = new User
        {
            Username = username,
            FullName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name.Trim() : username,
            Email = normalizedEmail,
            Avatar = !string.IsNullOrWhiteSpace(payload.Picture) ? payload.Picture : null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
        };

        dbContext.Users.Add(user);
        // Lưu user trước để có user.Id thực từ DB
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = user.Id,
            Action = "register_google",
            Detail = "Registered via Google OAuth"
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var raw = request.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new Exception("Invalid refresh token.");
        }

        var hashed = HashRefreshToken(raw);
        var stored = await dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.Token == hashed || t.Token == raw, cancellationToken);

        if (stored == null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            throw new Exception("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        // Token đã bị xoay vòng: chỉ chấp nhận trong khoảng grace (trường hợp nhiều tab refresh song song),
        // tái sử dụng sau khoảng grace là dấu hiệu bất thường -> từ chối
        if (stored.RevokedAt.HasValue &&
            (DateTime.UtcNow - stored.RevokedAt.Value).TotalSeconds > ReuseGraceSeconds)
        {
            throw new Exception("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        var user = stored.User;
        if (user == null || !user.IsActive)
        {
            throw new Exception("Tài khoản bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        stored.RevokedAt ??= DateTime.UtcNow;

        var (newRefresh, newRawToken) = IssueRefreshToken(user.Id);

        // Dọn dẹp token cũ đã hết hạn/thu hồi quá lâu của user này
        // (token hiện tại vừa được thu hồi nên không bao giờ rơi vào mốc 7 ngày)
        var staleCutoff = DateTime.UtcNow.AddDays(-7);
        var staleTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id &&
                        (t.ExpiresAt <= staleCutoff || (t.RevokedAt != null && t.RevokedAt <= staleCutoff)))
            .ToListAsync(cancellationToken);
        dbContext.RefreshTokens.RemoveRange(staleTokens);

        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user, AccessTokenDuration);
        return new AuthResponse(token, user.FullName, user.Email, null, newRawToken, newRefresh.ExpiresAt);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var raw = refreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        var hashed = HashRefreshToken(raw);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => (t.Token == hashed || t.Token == raw) && t.RevokedAt == null, cancellationToken);
        if (stored == null) return;

        stored.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private (RefreshToken Entity, string RawToken) IssueRefreshToken(int userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var token = new RefreshToken
        {
            UserId = userId,
            Token = HashRefreshToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        };
        dbContext.RefreshTokens.Add(token);
        return (token, rawToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        CancellationToken cancellationToken,
        string? tempPassword = null)
    {
        var token = GenerateJwtToken(user, AccessTokenDuration);
        var (refresh, rawToken) = IssueRefreshToken(user.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            token,
            user.FullName,
            user.Email,
            tempPassword,
            rawToken,
            refresh.ExpiresAt);
    }

    private string GenerateJwtToken(User user, TimeSpan? duration = null)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("level", user.Level ?? "A1"),
            new Claim(ClaimTypes.Role, "user")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(duration ?? AccessTokenDuration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateAdminJwtToken(Admin admin, TimeSpan? duration = null)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, admin.FullName),
            new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            new Claim("role", string.IsNullOrWhiteSpace(admin.Role) ? "admin" : admin.Role),
            new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(admin.Role) ? "admin" : admin.Role),
            new Claim("admin_id", admin.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(duration ?? AdminTokenDuration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateSecureRandomPassword(int length = 16)
    {
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = lower + upper + digits + special;

        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        chars[0] = lower[bytes[0] % lower.Length];
        chars[1] = upper[bytes[1] % upper.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];

        for (int i = 4; i < length; i++)
        {
            chars[i] = all[bytes[i] % all.Length];
        }

        var shuffleBytes = RandomNumberGenerator.GetBytes(length);
        for (int i = length - 1; i > 0; i--)
        {
            int j = shuffleBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
