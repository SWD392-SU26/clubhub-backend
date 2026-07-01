using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;
using ClubHub.API.Entities;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(Guid userId, string title, string content, string? type = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type
        });
        await _db.SaveChangesAsync();
    }

    public async Task CreateManyAsync(IEnumerable<Guid> userIds, string title, string content, string? type = null)
    {
        var notifications = userIds.Select(id => new Notification
        {
            UserId = id,
            Title = title,
            Content = content,
            Type = type
        });

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var ordered = query.OrderByDescending(n => n.CreatedAt);

        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Content, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return new PagedResult<NotificationDto>(items, page, pageSize, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n =>
            n.Id == notificationId && n.UserId == userId);

        if (notification == null) return ApiResult<bool>.Failure("Thông báo không tồn tại.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return ApiResult<bool>.Success(true);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        if (unread.Count > 0)
            await _db.SaveChangesAsync();

        return unread.Count;
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n =>
            n.Id == notificationId && n.UserId == userId);

        if (notification == null) return ApiResult<bool>.Failure("Thông báo không tồn tại.");

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }
}
