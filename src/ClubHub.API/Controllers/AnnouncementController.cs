using System.Security.Claims;
using ClubHub.API.DTOs.Announcement;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/clubs/{clubId:guid}/announcements")]
[Authorize]
[Produces("application/json")]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementController(IAnnouncementService announcementService)
        => _announcementService = announcementService;

    /// <summary>[Club Member] View internal club announcements</summary>
    [HttpGet]
    public async Task<IActionResult> GetClubAnnouncements(Guid clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _announcementService.GetClubAnnouncementsAsync(clubId, GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Create an internal club announcement</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid clubId, [FromBody] CreateAnnouncementRequest request)
    {
        var result = await _announcementService.CreateAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetClubAnnouncements), new { clubId }, ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Update an internal club announcement</summary>
    [HttpPut("{announcementId:guid}")]
    public async Task<IActionResult> Update(Guid clubId, Guid announcementId, [FromBody] UpdateAnnouncementRequest request)
    {
        var result = await _announcementService.UpdateAsync(clubId, announcementId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Archive an internal club announcement</summary>
    [HttpDelete("{announcementId:guid}")]
    public async Task<IActionResult> Archive(Guid clubId, Guid announcementId)
    {
        var result = await _announcementService.ArchiveAsync(clubId, announcementId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
