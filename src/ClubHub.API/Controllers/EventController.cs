using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Event;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Produces("application/json")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService) => _eventService = eventService;

    /// <summary>Lấy danh sách sự kiện của CLB</summary>
    [HttpGet("api/clubs/{clubId:guid}/events")]
    public async Task<IActionResult> GetClubEvents(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _eventService.GetClubEventsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem chi tiết sự kiện</summary>
    [HttpGet("api/events/{eventId:guid}")]
    public async Task<IActionResult> GetById(Guid eventId)
    {
        var result = await _eventService.GetEventByIdAsync(eventId);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Sự kiện không tồn tại."));
    }

    /// <summary>[Club Admin] Tạo sự kiện mới</summary>
    [HttpPost("api/clubs/{clubId:guid}/events")]
    [Authorize]
    public async Task<IActionResult> Create(Guid clubId, [FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateEventAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { eventId = result.Data!.Id }, ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Cập nhật sự kiện</summary>
    [HttpPut("api/events/{eventId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid eventId, [FromBody] UpdateEventRequest request)
    {
        var result = await _eventService.UpdateEventAsync(eventId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Hủy/xóa sự kiện</summary>
    [HttpDelete("api/events/{eventId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid eventId)
    {
        var result = await _eventService.DeleteEventAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Member] Đăng ký tham gia sự kiện</summary>
    [HttpPost("api/events/{eventId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> Register(Guid eventId)
    {
        var result = await _eventService.RegisterForEventAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data, "Đăng ký thành công."))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Member] Hủy đăng ký sự kiện</summary>
    [HttpDelete("api/events/{eventId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> CancelRegister(Guid eventId)
    {
        var result = await _eventService.CancelRegistrationAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Check-in thành viên vào sự kiện</summary>
    [HttpPost("api/events/{eventId:guid}/checkin/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> CheckIn(Guid eventId, Guid userId)
    {
        var result = await _eventService.CheckInAsync(eventId, userId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xem danh sách đăng ký của sự kiện</summary>
    [HttpGet("api/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> GetRegistrations(Guid eventId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _eventService.GetEventRegistrationsAsync(eventId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem sự kiện mình đã đăng ký</summary>
    [HttpGet("api/my-events")]
    [Authorize]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var result = await _eventService.GetMyRegistrationsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
