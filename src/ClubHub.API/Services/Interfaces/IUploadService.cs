using ClubHub.API.DTOs.Upload;

namespace ClubHub.API.Services.Interfaces;

public interface IUploadService
{
    Task<ApiResult<UploadResponseDto>> UploadAsync(IFormFile file, string purpose, HttpRequest request);
}
