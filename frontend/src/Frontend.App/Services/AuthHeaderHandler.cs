using System.Net;
using System.Net.Http.Headers;

namespace Frontend.App.Services;

/// <summary>
/// Tự động gắn Authorization: Bearer token vào mọi request gọi backend.
/// - Lấy token qua TokenRefreshService (tự refresh ngầm nếu token hết hạn).
/// - Khi gặp 401 (token bị thu hồi/hết hạn giữa chừng): refresh một lần rồi gửi lại request.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly TokenRefreshService _tokenService;

    public AuthHeaderHandler(TokenRefreshService tokenService)
    {
        _tokenService = tokenService;
        // Bắt buộc: DelegatingHandler phải có handler bên trong để gửi request đi tiếp
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Các endpoint auth tự nó không cần retry (refresh/logout là anonymous)
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }

        // Token có thể vừa hết hạn/bị thu hồi: làm mới một lần rồi thử lại
        if (!await _tokenService.RefreshAsync(force: true))
            return response;

        response.Dispose();

        using var retryRequest = await CloneRequestAsync(request);
        await AttachTokenAsync(retryRequest);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        try
        {
            var token = await _tokenService.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Chưa đăng nhập / lỗi đọc storage -> gửi request như khách
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content != null)
        {
            var body = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
