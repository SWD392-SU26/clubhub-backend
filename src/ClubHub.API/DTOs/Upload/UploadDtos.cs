namespace ClubHub.API.DTOs.Upload;

public record UploadResponseDto(
    string FileName,
    string OriginalFileName,
    string ContentType,
    long Size,
    string Url
);
