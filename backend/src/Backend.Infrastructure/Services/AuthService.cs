using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Infrastructure.Services;

public class AuthService(AppDbContext dbContext, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new Exception("Email này đã được sử dụng. Vui lòng dùng email khác hoặc đăng nhập.");
        }

        // Auto-generate a unique username from email (internal, not shown to user)
        var baseUsername = normalizedEmail.Split('@')[0];
        var username = baseUsername;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{baseUsername}{suffix++}";
        }

        var user = new User
        {
            Username = username,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? username : request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);
        return new AuthResponse(token, user.FullName, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            throw new Exception("Tài khoản bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        string? tempPassword = null;
        if ((DateTime.UtcNow - user.PasswordChangedAt).TotalDays > 30)
        {
            tempPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
        }

        user.LastLoginAt = DateTime.UtcNow;
        
        dbContext.UserActivityLogs.Add(new UserActivityLog
        {
            UserId = user.Id,
            Action = "login",
            Detail = tempPassword != null ? "Password auto-reset due to 30 days policy" : "Normal login"
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user, 30);
        return new AuthResponse(token, user.FullName, user.Email, tempPassword);
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
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

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == payload.Email.Trim().ToLowerInvariant(), cancellationToken);
        
        if (user == null)
        {
            // Do not auto-register. Throw a specific error so frontend can redirect to registration.
            throw new Exception($"UserNotRegistered|{payload.Email}");
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

        var token = GenerateJwtToken(user, 1.0 / 24.0); // 1 hour for Google login
        return new AuthResponse(token, user.FullName, user.Email);
    }

    private string GenerateJwtToken(User user, double expireDays = 30)
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
            new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Role) ? "user" : user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expireDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRandomPassword()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8) + "@1Aa";
    }
}
