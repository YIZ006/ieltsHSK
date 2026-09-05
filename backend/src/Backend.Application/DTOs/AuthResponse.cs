namespace Backend.Application.DTOs;

public record AuthResponse(
    string Token,
    string FullName,
    string Email,
    string? TempPassword = null,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAt = null);

/// <summary>Body cho endpoint /api/auth/refresh và /api/auth/logout.</summary>
public record RefreshRequest(string RefreshToken);
