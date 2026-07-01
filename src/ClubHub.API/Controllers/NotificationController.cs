using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Notification;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
        => _notificationService = notificationService;

    /// <summary>Lấy danh sách thông báo của mình (có phân trang)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), 200)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        var result = await _notificationService.GetMyNotificationsAsync(GetUserId(), page, pageSize, unreadOnly);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Đếm số thông báo chưa đọc</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), 200)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(GetUserId());
        return Ok(ApiResponse.Ok(new UnreadCountDto(count)));
    }

    /// <summary>Đánh dấu một thông báo là đã đọc</summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(id, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Đánh dấu tất cả thông báo là đã đọc</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var count = await _notificationService.MarkAllAsReadAsync(GetUserId());
        return Ok(ApiResponse.Ok(count, $"Đã đánh dấu {count} thông báo là đã đọc."));
    }

    /// <summary>Xóa một thông báo</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _notificationService.DeleteAsync(id, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
