namespace Backend.Application.DTOs;

public record UpdateProfileRequest(
    string? FullName = null,
    string? Username = null,
    string? Avatar = null,
    string? Level = null
);
