using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ClubHub.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClubHub.API.Services.Implementations;

/// <summary>Upload file lên AWS S3 và trả về URL công khai</summary>
public class AwsS3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;

    public AwsS3StorageService(IConfiguration config)
    {
        var region = RegionEndpoint.GetBySystemName(config["AWS:Region"] ?? "ap-southeast-1");
        _s3Client = new AmazonS3Client(
            config["AWS:AccessKeyId"],
            config["AWS:SecretAccessKey"],
            region);
        _bucketName = config["AWS:BucketName"]!;
        // CloudFront hoặc S3 public URL
        _baseUrl = config["AWS:BaseUrl"] ?? $"https://{_bucketName}.s3.amazonaws.com";
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder = "uploads")
    {
        var key = $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request);

        return $"{_baseUrl.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string fileUrlOrKey)
    {
        // Extract key from URL if a full URL is provided
        var key = fileUrlOrKey.StartsWith("http")
            ? fileUrlOrKey.Replace(_baseUrl.TrimEnd('/') + "/", "")
            : fileUrlOrKey;

        await _s3Client.DeleteObjectAsync(_bucketName, key);
    }

    private static string SanitizeFileName(string fileName)
        => string.Concat(Path.GetFileNameWithoutExtension(fileName)
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            + Path.GetExtension(fileName);
}
