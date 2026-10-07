namespace Backend.Application.DTOs;

public record LoginRequest(string? UsernameOrEmail, string Password, string? Email = null, string? CaptchaToken = null)
{
    public string ResolvedUsernameOrEmail => !string.IsNullOrWhiteSpace(UsernameOrEmail) ? UsernameOrEmail : (Email ?? "");
}
