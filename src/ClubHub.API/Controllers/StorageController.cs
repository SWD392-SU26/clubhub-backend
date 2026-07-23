using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/storage")]
[Authorize]
[Produces("application/json")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    // Giới hạn kích thước: 5 MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // Các loại file hình ảnh được phép
    private static readonly string[] AllowedImageTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public StorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>Upload ảnh lên AWS S3, trả về URL công khai</summary>
    /// <param name="file">File ảnh (jpg/png/webp/gif, tối đa 5MB)</param>
    /// <param name="folder">Thư mục lưu trữ: avatars | clubs | events | proposals (mặc định: uploads)</param>
    [HttpPost("upload-image")]
    [ProducesResponseType(typeof(ApiResponse<UploadResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> UploadImage(
        IFormFile file,
        [FromQuery] string folder = "uploads")
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("Vui lòng chọn file để upload."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(ApiResponse.Fail("File quá lớn. Giới hạn là 5MB."));

        if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(ApiResponse.Fail("Chỉ hỗ trợ định dạng ảnh: jpg, png, webp, gif."));

        await using var stream = file.OpenReadStream();
        var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType, folder);

        return Ok(ApiResponse.Ok(new UploadResultDto(url), "Upload thành công."));
    }
}

public record UploadResultDto(string Url);
