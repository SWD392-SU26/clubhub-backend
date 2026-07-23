using System.Security.Claims;
using ClubHub.API.DTOs.Activity;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Produces("application/json")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService) => _activityService = activityService;

    // ═════════════════════════════════════════════════════════════════════════
    // Public endpoints
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Lấy danh sách hoạt động nội bộ của CLB (public)</summary>
    [HttpGet("api/clubs/{clubId:guid}/activities")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ActivityDto>>), 200)]
    public async Task<IActionResult> GetClubActivities(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _activityService.GetClubActivitiesAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem chi tiết hoạt động nội bộ (public — Club Admin thấy danh sách registrants)</summary>
    [HttpGet("api/activities/{activityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid activityId)
    {
        var currentUserId = GetUserIdOrNull();
        var result = await _activityService.GetActivityByIdAsync(activityId, currentUserId);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Hoạt động không tồn tại."));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Club Admin endpoints
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>[Club Admin] Tạo hoạt động nội bộ mới</summary>
    [HttpPost("api/clubs/{clubId:guid}/activities")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ActivityDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create(Guid clubId, [FromBody] CreateActivityRequest request)
    {
        var result = await _activityService.CreateActivityAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { activityId = result.Data!.Id }, ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Cập nhật hoạt động nội bộ</summary>
    [HttpPut("api/activities/{activityId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ActivityDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Update(Guid activityId, [FromBody] UpdateActivityRequest request)
    {
        var result = await _activityService.UpdateActivityAsync(activityId, request, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data!)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Hủy hoạt động nội bộ</summary>
    [HttpDelete("api/activities/{activityId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Delete(Guid activityId)
    {
        var result = await _activityService.DeleteActivityAsync(activityId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Check-in cho member</summary>
    [HttpPost("api/activities/{activityId:guid}/checkin/{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CheckIn(Guid activityId, Guid userId)
    {
        var result = await _activityService.CheckInAsync(activityId, userId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Member endpoints
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>[Member] Đăng ký tham gia hoạt động</summary>
    [HttpPost("api/activities/{activityId:guid}/register")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Register(Guid activityId, [FromBody] RegisterActivityRequest? request)
    {
        var result = await _activityService.RegisterAsync(activityId, GetUserId(), request);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Member] Hủy đăng ký tham gia hoạt động</summary>
    [HttpDelete("api/activities/{activityId:guid}/register")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CancelRegistration(Guid activityId)
    {
        var result = await _activityService.CancelRegistrationAsync(activityId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Member] Xem danh sách hoạt động đã đăng ký</summary>
    [HttpGet("api/my-activities")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MyRegisteredActivityDto>>), 200)]
    public async Task<IActionResult> GetMyRegisteredActivities(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _activityService.GetMyRegisteredActivitiesAsync(GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid? GetUserIdOrNull()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }
}
