namespace ClubHub.API.Services.Interfaces;

/// <summary>Service upload file lên AWS S3, trả về URL công khai</summary>
public interface IStorageService
{
    /// <summary>Upload file và trả về URL công khai</summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder = "uploads");

    /// <summary>Xóa file theo URL hoặc key</summary>
    Task DeleteAsync(string fileUrlOrKey);
}
