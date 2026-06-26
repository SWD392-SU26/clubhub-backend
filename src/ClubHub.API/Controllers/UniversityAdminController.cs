using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

/// <summary>Các thao tác dành riêng cho University Admin</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "UniversityAdmin")]
[Produces("application/json")]
public class UniversityAdminController : ControllerBase
{
    private readonly IClubService _clubService;
    private readonly IAdminStatisticsService _statisticsService;
    private readonly IMembershipService _membershipService;

    public UniversityAdminController(
        IClubService clubService,
        IAdminStatisticsService statisticsService,
        IMembershipService membershipService)
    {
        _clubService = clubService;
        _statisticsService = statisticsService;
        _membershipService = membershipService;
    }

    /// <summary>Xem toàn bộ CLB (mọi trạng thái)</summary>
    [HttpGet("clubs")]
    public async Task<IActionResult> GetAllClubs([FromQuery] ClubFilterRequest filter)
    {
        var result = await _clubService.GetAdminClubsAsync(filter);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>University-level activity statistics</summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _statisticsService.GetUniversityStatisticsAsync();
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Activity statistics for one club</summary>
    [HttpGet("clubs/{clubId:guid}/statistics")]
    public async Task<IActionResult> GetClubStatistics(Guid clubId)
    {
        var result = await _statisticsService.GetClubStatisticsAsync(clubId);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Club does not exist."));
    }

    /// <summary>Tạo CLB trực tiếp (không qua hồ sơ)</summary>
    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClub([FromBody] CreateClubRequest request)
    {
        var adminId = GetUserId();
        var result = await _clubService.CreateClubAsync(request, adminId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAllClubs), ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Ẩn CLB</summary>
    [HttpPut("clubs/{clubId:guid}/hide")]
    public async Task<IActionResult> HideClub(Guid clubId)
    {
        var result = await _clubService.HideClubAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Khóa CLB</summary>
    [HttpPut("clubs/{clubId:guid}/lock")]
    public async Task<IActionResult> LockClub(Guid clubId)
    {
        var result = await _clubService.LockClubAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xóa mềm CLB</summary>
    [HttpDelete("clubs/{clubId:guid}")]
    public async Task<IActionResult> SoftDeleteClub(Guid clubId)
    {
        var result = await _clubService.DeleteClubAsync(clubId, GetUserId(), hardDelete: false);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xóa cứng CLB</summary>
    [HttpDelete("clubs/{clubId:guid}/hard")]
    public async Task<IActionResult> HardDeleteClub(Guid clubId)
    {
        var result = await _clubService.DeleteClubAsync(clubId, GetUserId(), hardDelete: true);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[University Admin] Transfer club president role to another approved member</summary>
    [HttpPut("clubs/{clubId:guid}/transfer-manager")]
    public async Task<IActionResult> TransferManager(Guid clubId, [FromBody] TransferAdminRequest request)
    {
        var result = await _membershipService.TransferAdminAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}
