using System.Net.Http.Json;

namespace Frontend.App.Services;

public class AdminUserService
{
    private readonly HttpClient _httpClient;

    public AdminUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GetUsersResult> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/admin/users", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new GetUsersResult
                {
                    IsUnauthorized = true,
                    ErrorMessage = "Phiên đăng nhập không có quyền Quản trị viên (Admin) hoặc đã hết hạn."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new GetUsersResult
                {
                    ErrorMessage = $"Lỗi máy chủ: {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: cancellationToken);
            return new GetUsersResult
            {
                IsSuccess = true,
                Users = users ?? new List<UserDto>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUserService] Error fetching users: {ex.Message}");
            return new GetUsersResult
            {
                ErrorMessage = $"Lỗi kết nối API: {ex.Message}"
            };
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/admin/users/{userId}/toggle-active", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUserService] Error toggling status: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(int userId, object updateReq, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}", updateReq, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUserService] Error updating user: {ex.Message}");
            return false;
        }
    }

    public async Task<ResetPasswordResult?> ResetPasswordAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/admin/users/{userId}/reset-password", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ResetPasswordResult>(cancellationToken: cancellationToken);
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUserService] Error resetting password: {ex.Message}");
            return null;
        }
    }

    public async Task<List<UserLogDto>> GetUserLogsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _httpClient.GetFromJsonAsync<List<UserLogDto>>($"api/admin/users/{userId}/logs", cancellationToken);
            return logs ?? new List<UserLogDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUserService] Error fetching logs: {ex.Message}");
            return new List<UserLogDto>();
        }
    }
}

public class GetUsersResult
{
    public bool IsSuccess { get; set; }
    public bool IsUnauthorized { get; set; }
    public string? ErrorMessage { get; set; }
    public List<UserDto> Users { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "user";
    public string Level { get; set; } = "A1";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ResetPasswordResult
{
    public string TempPassword { get; set; } = "";
}
