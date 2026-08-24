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
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrWhiteSpace(token))
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
