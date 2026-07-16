using ClubHub.API.DTOs.Upload;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Implementations;

public class UploadService : IUploadService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private readonly IWebHostEnvironment _environment;

    public UploadService(IWebHostEnvironment environment) => _environment = environment;

    public async Task<ApiResult<UploadResponseDto>> UploadAsync(IFormFile file, string purpose, HttpRequest request)
    {
        if (file == null || file.Length == 0)
            return ApiResult<UploadResponseDto>.Failure("File is required.");

        purpose = NormalizePurpose(purpose);
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            return ApiResult<UploadResponseDto>.Failure("File extension is required.");

        var allowsDocuments = purpose is "proposal-file";
        var isAllowed = ImageExtensions.Contains(extension) ||
                        (allowsDocuments && DocumentExtensions.Contains(extension));

        if (!isAllowed)
            return ApiResult<UploadResponseDto>.Failure(allowsDocuments
                ? "Allowed file types are jpg, jpeg, png, webp, pdf, doc, and docx."
                : "Allowed image types are jpg, jpeg, png, and webp.");

        var maxSize = allowsDocuments ? 10 * 1024 * 1024 : 5 * 1024 * 1024;
        if (file.Length > maxSize)
            return ApiResult<UploadResponseDto>.Failure(allowsDocuments
                ? "Proposal files must be 10MB or smaller."
                : "Images must be 5MB or smaller.");

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var uploadRoot = Path.Combine(webRoot, "uploads", purpose);
        Directory.CreateDirectory(uploadRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadRoot, storedFileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{purpose}/{storedFileName}";
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var url = $"{baseUrl}{relativeUrl}";

        return ApiResult<UploadResponseDto>.Success(new UploadResponseDto(
            storedFileName,
            file.FileName,
            file.ContentType,
            file.Length,
            url));
    }

    private static string NormalizePurpose(string? purpose)
    {
        var normalized = (purpose ?? "general").Trim().ToLowerInvariant();
        return normalized switch
        {
            "avatar" => "avatar",
            "logo" => "logo",
            "cover" => "cover",
            "proposal-file" => "proposal-file",
            "founder-id" => "founder-id",
            _ => "general"
        };
    }
}
