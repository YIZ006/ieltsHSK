using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.App.Services;

public class CustomAuthStateProvider(ILocalStorageService localStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return AnonymousState;
            }

            var claims = ParseClaimsFromJwt(token);
            if (!claims.Any())
            {
                return AnonymousState;
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resolving auth state: {ex.Message}");
            return AnonymousState;
        }
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

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
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
                claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)));
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
}
