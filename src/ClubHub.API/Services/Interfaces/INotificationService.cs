using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;

namespace ClubHub.API.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize);
    Task<UnreadNotificationCountDto> GetUnreadCountAsync(Guid userId);
    Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId);
}
