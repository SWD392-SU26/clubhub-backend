using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.DTOs.Announcement;

public record CreateAnnouncementRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(2000)] string Content,
    bool IsPinned = false
);

public record UpdateAnnouncementRequest(
    [MaxLength(200)] string? Title,
    [MaxLength(2000)] string? Content,
    bool? IsPinned
);

public record AnnouncementDto(
    Guid Id,
    Guid ClubId,
    string Title,
    string Content,
    bool IsPinned,
    Guid CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
