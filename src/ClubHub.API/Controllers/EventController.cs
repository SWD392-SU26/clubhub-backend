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

    [HttpGet("api/events")]
    public async Task<IActionResult> GetPublicEvents([FromQuery] EventFilterRequest filter)
    {
        var result = await _eventService.GetPublicEventsAsync(filter, upcomingOnly: false);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("api/events/upcoming")]
    public async Task<IActionResult> GetUpcomingEvents([FromQuery] EventFilterRequest filter)
    {
        var result = await _eventService.GetPublicEventsAsync(filter, upcomingOnly: true);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("api/clubs/{clubId:guid}/events")]
    public async Task<IActionResult> GetClubEvents(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _eventService.GetClubEventsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("api/events/{eventId:guid}")]
    public async Task<IActionResult> GetById(Guid eventId)
    {
        var result = await _eventService.GetEventByIdAsync(eventId, TryGetUserId());
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Event does not exist."));
    }

    [HttpPost("api/clubs/{clubId:guid}/events")]
    [Authorize]
    public async Task<IActionResult> Create(Guid clubId, [FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateEventAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { eventId = result.Data!.Id }, ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPut("api/events/{eventId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid eventId, [FromBody] UpdateEventRequest request)
    {
        var result = await _eventService.UpdateEventAsync(eventId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpDelete("api/events/{eventId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid eventId)
    {
        var result = await _eventService.DeleteEventAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("api/events/{eventId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> Register(Guid eventId)
    {
        var result = await _eventService.RegisterForEventAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data, "Registered successfully."))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpDelete("api/events/{eventId:guid}/register")]
    [Authorize]
    public async Task<IActionResult> CancelRegister(Guid eventId)
    {
        var result = await _eventService.CancelRegistrationAsync(eventId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("api/events/{eventId:guid}/checkin/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> CheckIn(Guid eventId, Guid userId)
    {
        var result = await _eventService.CheckInAsync(eventId, userId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpGet("api/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> GetRegistrations(Guid eventId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _eventService.GetEventRegistrationsAsync(eventId, GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("api/my-events")]
    [Authorize]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var result = await _eventService.GetMyRegistrationsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
