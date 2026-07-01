namespace ClubHub.API.DTOs.Notification;

public record NotificationDto(
    Guid Id,
    string Title,
    string Content,
    string? Type,
    bool IsRead,
    DateTime CreatedAt
);

public record UnreadCountDto(int UnreadCount);
