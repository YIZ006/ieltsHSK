using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using System.Text;
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

        if (await dbContext.Users.AnyAsync(u => u.Username == request.Username || u.Email == normalizedEmail, cancellationToken))
        {
            throw new Exception("Username or Email already exists.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);
        return new AuthResponse(token, user.Username, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new Exception("Account is disabled. Please contact admin.");
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

        var token = GenerateJwtToken(user, 30); // 30 days for normal login
        return new AuthResponse(token, user.Username, user.Email, tempPassword);
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
        return new AuthResponse(token, user.Username, user.Email);
    }

    private string GenerateJwtToken(User user, double expireDays = 30)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("level", user.Level ?? "A1")
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
