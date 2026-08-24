namespace Backend.Application.DTOs;

public record AuthResponse(string Token, string Username, string Email, string? TempPassword = null);
