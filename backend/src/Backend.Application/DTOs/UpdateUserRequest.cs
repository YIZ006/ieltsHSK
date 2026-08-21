namespace Backend.Application.DTOs;

public record UpdateUserRequest(string Username, string Email, string Role, string Level);
