namespace Backend.Application.DTOs;

public record LoginRequest(string? UsernameOrEmail, string Password, string? Email = null)
{
    public string ResolvedUsernameOrEmail => !string.IsNullOrWhiteSpace(UsernameOrEmail) ? UsernameOrEmail : (Email ?? "");
}
