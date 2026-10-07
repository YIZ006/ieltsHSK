namespace Backend.Application.Abstractions;

public interface ICaptchaService
{
    Task<bool> VerifyCaptchaAsync(string? token, string? clientIp = null, CancellationToken cancellationToken = default);
}
