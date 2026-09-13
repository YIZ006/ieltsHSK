using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly TokenRefreshService _tokenRefreshService;

    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(ILocalStorageService localStorage, TokenRefreshService tokenRefreshService)
    {
        _localStorage = localStorage;
        _tokenRefreshService = tokenRefreshService;
        // Token được gia hạn ngầm / phiên chết ở bất kỳ đâu -> cập nhật trạng thái đăng nhập ngay lập tức
        _tokenRefreshService.TokensRefreshed += NotifyUserAuthentication;
        _tokenRefreshService.SessionExpired += NotifyUserLogout;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenRefreshService.AccessTokenKey);

            if (string.IsNullOrWhiteSpace(token) || IsJwtExpired(token))
            {
                return AnonymousState;
            }

            var claims = ParseClaimsFromJwt(token).ToList();
            if (!claims.Any())
            {
                return AnonymousState;
            }

            if (!TokenRefreshService.IsTokenExpired(token))
            {
                return CreateState(claims);
            }

            // Token hết hạn nhưng còn refresh token -> tự gia hạn để "ghi nhớ" phiên đăng nhập
            if (!await _tokenRefreshService.RefreshAsync())
            {
                return AnonymousState;
            }

            var newToken = await _localStorage.GetItemAsync<string>(TokenRefreshService.AccessTokenKey);
            if (string.IsNullOrWhiteSpace(newToken))
            {
                return AnonymousState;
            }

            claims = ParseClaimsFromJwt(newToken).ToList();
            return claims.Any() ? CreateState(claims) : AnonymousState;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resolving auth state: {ex.Message}");
            return AnonymousState;
        }
    }

    private static AuthenticationState CreateState(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public void NotifyUserAuthentication(string token)
    {
        try
        {
            var claims = ParseClaimsFromJwt(token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying user auth: {ex.Message}");
            NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState));
        }
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(AnonymousState);
        NotifyAuthenticationStateChanged(authState);
    }

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        if (string.IsNullOrWhiteSpace(jwt)) return claims;

        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return claims;

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    var val = kvp.Value?.ToString() ?? string.Empty;
                    claims.Add(new Claim(kvp.Key, val));

                    if (kvp.Key == "role" || kvp.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    {
                        if (kvp.Key != ClaimTypes.Role)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, val));
                        }
                    }

                    if (kvp.Key == "unique_name" || kvp.Key == "name" || kvp.Key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                    {
                        if (kvp.Key != ClaimTypes.Name)
                        {
                            claims.Add(new Claim(ClaimTypes.Name, val));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JWT claims: {ex.Message}");
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public static bool IsJwtExpired(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return true;
        try
        {
            var claims = ParseClaimsFromJwt(jwt);
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (long.TryParse(expClaim, out var expSeconds))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                return expDate <= DateTimeOffset.UtcNow;
            }
        }
        catch { }
        return false;
    }
}
