using System.Net.Http;
using Blazored.LocalStorage;

namespace Frontend.App.Services;

/// <summary>
/// Tự động gắn Authorization: Bearer token (từ localStorage "authToken") vào mọi request.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public AuthHeaderHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        // Bắt buộc: DelegatingHandler phải có handler bên trong để gửi request đi tiếp
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var path = request.RequestUri?.AbsolutePath?.ToLowerInvariant() ?? "";
            string? token = null;

            bool isAdminEndpoint = path.Contains("/api/admin") 
                || (path.Contains("/api/mock-tests") && request.Method != HttpMethod.Get)
                || path.Contains("/api/mock-tests/upload");

            if (isAdminEndpoint)
            {
                // Ưu tiên token riêng của Admin
                token = await _localStorage.GetItemAsync<string>("admin_authToken");
                if (string.IsNullOrWhiteSpace(token) || CustomAuthStateProvider.IsJwtExpired(token))
                {
                    token = await _localStorage.GetItemAsync<string>("authToken");
                }
            }
            else
            {
                // API học viên dùng token riêng của học viên
                token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrWhiteSpace(token) || CustomAuthStateProvider.IsJwtExpired(token))
                {
                    var adm = await _localStorage.GetItemAsync<string>("admin_authToken");
                    if (!string.IsNullOrWhiteSpace(adm) && !CustomAuthStateProvider.IsJwtExpired(adm))
                    {
                        token = adm;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(token) && !CustomAuthStateProvider.IsJwtExpired(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Chưa đăng nhập / lỗi đọc storage -> gửi request như khách
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
