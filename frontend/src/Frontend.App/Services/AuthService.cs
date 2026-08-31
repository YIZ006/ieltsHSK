using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 50 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 30 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Tên đăng nhập chỉ được bao gồm chữ cái và chữ số (không chứa ký tự đặc biệt hay khoảng trắng).")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập hoặc Email.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    public string Email
    {
        get => UsernameOrEmail;
        set => UsernameOrEmail = value;
    }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CheckUsernameResult
{
    public bool IsTaken { get; set; }
}

public class AuthService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
{
    // Tất cả các key localStorage liên quan đến user – phải xóa khi đổi tài khoản
    private static readonly string[] UserStorageKeys =
    [
        // Auth
        "authToken",
        // IELTS
        "ielts_level",
        "ielts-exam-submissions",
        // HSK
        "hsk_level",
        "hsk_progress_migrated",
        "hsk_learned_HSK1",
        "hsk_learned_HSK2",
        "hsk_learned_HSK3",
        "hsk_learned_HSK4",
        "hsk_learned_HSK5",
        "hsk_learned_HSK6",
        "hsk_learned_HSK7",
        "hsk_learned_HSK8",
        "hsk_learned_HSK9",
        // Profile & Streak
        "user_profile",
        "streak_active_days",
        // TOEIC
        "toeic_flashcards_v1",
    ];

    private async Task ClearUserDataAsync()
    {
        foreach (var key in UserStorageKeys)
            await localStorage.RemoveItemAsync(key);
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/register", request);
            if (response.IsSuccessStatusCode)
            {
                return (true, string.Empty);
            }

            var error = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(error))
            {
                error = "Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.";
            }
            return (false, error.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, "Lỗi kết nối máy chủ: " + ex.Message);
        }
    }

    public async Task<bool> CheckUsernameExistsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        try
        {
            var res = await httpClient.GetFromJsonAsync<CheckUsernameResult>($"api/auth/check-username?username={Uri.EscapeDataString(username)}");
            return res?.IsTaken ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return false;

            // Xóa dữ liệu user cũ trước khi lưu token mới
            await ClearUserDataAsync();
            await localStorage.SetItemAsync("authToken", result.Token);
            ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, bool IsAdmin, string ErrorMessage)> AdminLoginAsync(LoginRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                var errContent = await response.Content.ReadAsStringAsync();
                return (false, false, string.IsNullOrWhiteSpace(errContent) ? "Email hoặc mật khẩu không chính xác." : errContent.Trim('"'));
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                return (false, false, "Phản hồi không hợp lệ từ máy chủ.");
            }

            // Kiểm tra claim role trong JWT
            var claims = CustomAuthStateProvider.ParseClaimsFromJwt(result.Token);
            var roleClaim = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")?.Value;

            if (!string.Equals(roleClaim, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, false, "Tài khoản của bạn không có quyền Quản trị viên (Admin). Vui lòng sử dụng tài khoản được cấp phép.");
            }

            // Xóa dữ liệu user cũ trước khi lưu token mới
            await ClearUserDataAsync();
            await localStorage.SetItemAsync("authToken", result.Token);
            ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
            return (true, true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, false, "Lỗi kết nối máy chủ: " + ex.Message);
        }
    }

    public async Task<(bool Success, string ErrorMessage)> LoginWithGoogleAsync(string idToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/google-login", new { IdToken = idToken });
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return (false, "Invalid response from server.");

            // Xóa dữ liệu user cũ trước khi lưu token mới
            await ClearUserDataAsync();
            await localStorage.SetItemAsync("authToken", result.Token);
            ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterWithGoogleAsync(string idToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/google-register", new { IdToken = idToken });
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return (false, "Invalid response from server.");

            // Xóa dữ liệu user cũ trước khi lưu token mới
            await ClearUserDataAsync();
            await localStorage.SetItemAsync("authToken", result.Token);
            ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        await ClearUserDataAsync();
        ((CustomAuthStateProvider)authStateProvider).NotifyUserLogout();
    }
}
