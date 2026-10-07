using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Services;

public class CloudflareTurnstileService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudflareTurnstileService> _logger;

    public CloudflareTurnstileService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CloudflareTurnstileService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> VerifyCaptchaAsync(string? token, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue<bool?>("Turnstile:Enabled") ?? true;
        if (!enabled)
        {
            return true;
        }

        // Test key chuẩn của Cloudflare: Luôn trả về thành công khi thử nghiệm
        var secretKey = _configuration["Turnstile:SecretKey"]
            ?? Environment.GetEnvironmentVariable("TURNSTILE_SECRET_KEY")
            ?? "1x0000000000000000000000000000000AA";

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Turnstile verification failed: Captcha token is empty.");
            return false;
        }

        try
        {
            var postData = new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            };

            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                postData["remoteip"] = clientIp;
            }

            var content = new FormUrlEncodedContent(postData);

            var response = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile API responded with HTTP {StatusCode}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: cancellationToken);
            if (result?.Success == true)
            {
                return true;
            }

            _logger.LogWarning("Turnstile verification rejected token. Error codes: {Errors}",
                result?.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while verifying Cloudflare Turnstile token.");
            var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
            return env.Equals("Development", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; } = [];

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}
