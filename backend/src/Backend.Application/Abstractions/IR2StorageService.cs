namespace Backend.Application.Abstractions;

public interface IR2StorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> UploadPrivateAudioAsync(Stream fileStream, string key, string contentType, CancellationToken cancellationToken = default);
    Task<string> UploadPrivateFileAsync(Stream fileStream, string key, string contentType = "application/json", CancellationToken cancellationToken = default);
    Task<Stream?> GetPrivateFileStreamAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeletePrivateFileAsync(string key, CancellationToken cancellationToken = default);
}
