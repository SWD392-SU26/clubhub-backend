using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;

namespace ClubHub.API.Services.Interfaces;

public interface INotificationService
{
    /// <summary>Tạo thông báo cho một người dùng.</summary>
    Task CreateAsync(Guid userId, string title, string content, string? type = null);

    /// <summary>Tạo cùng một thông báo cho nhiều người dùng.</summary>
    Task CreateManyAsync(IEnumerable<Guid> userIds, string title, string content, string? type = null);

    /// <summary>Lấy danh sách thông báo của người dùng (có phân trang).</summary>
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly);

    /// <summary>Đếm số thông báo chưa đọc.</summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>Đánh dấu một thông báo là đã đọc.</summary>
    Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>Đánh dấu tất cả thông báo là đã đọc.</summary>
    Task<int> MarkAllAsReadAsync(Guid userId);

    /// <summary>Xóa một thông báo.</summary>
    Task<ApiResult<bool>> DeleteAsync(Guid notificationId, Guid userId);
}
