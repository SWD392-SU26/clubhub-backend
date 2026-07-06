using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;

namespace ClubHub.API.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize);
    Task<UnreadCountDto> GetUnreadCountAsync(Guid userId);
    Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId);
    Task SendNotificationAsync(Guid userId, string title, string content, string? type = null);
    Task SendBulkNotificationAsync(List<Guid> userIds, string title, string content, string? type = null);
}
