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

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), 200)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetMyNotificationsAsync(GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountDto>), 200)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _notificationService.GetUnreadCountAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(id, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : NotFound(ApiResponse.Fail(result.Error!));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync(GetUserId());
        return Ok(ApiResponse.Ok(result.Data));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
