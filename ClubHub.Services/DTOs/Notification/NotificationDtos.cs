using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.DTOs.Notification;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateNotificationRequest(
    [Required] Guid UserId,
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(1000)] string Content,
    string? Type
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record NotificationDto(
    Guid Id,
    string Title,
    string Content,
    string? Type,
    bool IsRead,
    DateTime CreatedAt
);

public record UnreadCountDto(int Count);
