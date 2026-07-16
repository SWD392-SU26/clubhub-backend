using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Content, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return new PagedResult<NotificationDto>(items, page, pageSize, total);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(Guid userId)
    {
        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return new UnreadNotificationCountDto(count);
    }

    public async Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return ApiResult<bool>.Failure("Notification does not exist.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
            notification.IsRead = true;

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }
}
