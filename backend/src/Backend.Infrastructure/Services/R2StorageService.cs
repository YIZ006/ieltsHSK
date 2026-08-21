using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Backend.Infrastructure.Services;

public class R2StorageService : IR2StorageService
{
    private readonly IConfiguration _config;
    
    public R2StorageService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var accessKey = _config["CloudflareR2:AccessKey"];
        var secretKey = _config["CloudflareR2:SecretKey"];
        var endpoint = _config["CloudflareR2:Endpoint"]; // e.g. https://<accountid>.r2.cloudflarestorage.com
        var bucketName = _config["CloudflareR2:BucketName"];
        var publicUrlBase = _config["CloudflareR2:PublicUrlBase"]; // e.g. https://pub-xxx.r2.dev

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "auto",
            ForcePathStyle = true // Bắt buộc đối với Cloudflare R2 để URL không bị sai định dạng
        };

        using var client = new AmazonS3Client(accessKey, secretKey, config);

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        var response = await client.PutObjectAsync(putRequest, cancellationToken);
        
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            return $"{publicUrlBase}/{fileName}";
        }
        
        throw new Exception("Lỗi khi upload file lên R2");
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        var publicUrlBase = _config["CloudflareR2:PublicUrlBase"];
        string fileName = "";
        if (!string.IsNullOrEmpty(publicUrlBase) && fileUrl.StartsWith(publicUrlBase, StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileUrl.Substring(publicUrlBase.Length).TrimStart('/');
        }
        else if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            fileName = uri.AbsolutePath.TrimStart('/');
        }
        else
        {
            fileName = fileUrl.TrimStart('/');
        }

        if (string.IsNullOrEmpty(fileName)) return false;

        var accessKey = _config["CloudflareR2:AccessKey"];
        var secretKey = _config["CloudflareR2:SecretKey"];
        var endpoint = _config["CloudflareR2:Endpoint"];
        var bucketName = _config["CloudflareR2:BucketName"];

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "auto",
            ForcePathStyle = true
        };

        using var client = new AmazonS3Client(accessKey, secretKey, config);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = fileName
        };

        try
        {
            var response = await client.DeleteObjectAsync(deleteRequest, cancellationToken);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent || response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file from R2: {ex.Message}");
            return false;
        }
    }
}
