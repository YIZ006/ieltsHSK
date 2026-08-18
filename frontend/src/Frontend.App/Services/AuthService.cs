using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản.")]
    [StringLength(15, MinimumLength = 6, ErrorMessage = "Tài khoản phải từ 6 đến 15 ký tự.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[^%&*$#@^]{8,15}$", 
        ErrorMessage = "Mật khẩu từ 8-15 ký tự, gồm cả chữ và số, không chứa %^&*$#@.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AuthService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
{
    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result == null) return false;

        await localStorage.SetItemAsync("authToken", result.Token);
        ((CustomAuthStateProvider)authStateProvider).NotifyUserAuthentication(result.Token);
        return true;
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
        await localStorage.RemoveItemAsync("authToken");
        await localStorage.RemoveItemAsync("ielts_level");
        ((CustomAuthStateProvider)authStateProvider).NotifyUserLogout();
    }
}
