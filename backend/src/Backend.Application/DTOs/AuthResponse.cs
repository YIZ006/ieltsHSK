namespace Backend.Application.DTOs;

public record AuthResponse(string Token, string FullName, string Email, string? TempPassword = null);
