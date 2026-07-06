using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;
using ClubHub.API.Entities;
using ClubHub.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Notifications.Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Content, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return new PagedResult<NotificationDto>(items, page, pageSize, total);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(Guid userId)
    {
        var count = await _uow.Notifications.CountAsync(n =>
            n.UserId == userId && !n.IsRead);
        return new UnreadCountDto(count);
    }

    public async Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notif = await _uow.Notifications.FirstOrDefaultAsync(n =>
            n.Id == notificationId && n.UserId == userId);

        if (notif == null)
            return ApiResult<bool>.Failure("Thông báo không tồn tại.");

        notif.IsRead = true;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task SendNotificationAsync(Guid userId, string title, string content, string? type = null)
    {
        _uow.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type
        });
        await _uow.SaveChangesAsync();
    }

    public async Task SendBulkNotificationAsync(List<Guid> userIds, string title, string content, string? type = null)
    {
        foreach (var userId in userIds)
        {
            _uow.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type
            });
        }
        await _uow.SaveChangesAsync();
    }
}
