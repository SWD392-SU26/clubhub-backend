using System.Security.Claims;
using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/clubs")]
[Produces("application/json")]
public class ClubController : ControllerBase
{
    private readonly IClubService _clubService;
    private readonly IAdminStatisticsService _statisticsService;

    public ClubController(IClubService clubService, IAdminStatisticsService statisticsService)
    {
        _clubService = clubService;
        _statisticsService = statisticsService;
    }

    /// <summary>Lấy danh sách CLB (có filter + phân trang)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClubSummaryDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ClubFilterRequest filter)
    {
        var result = await _clubService.GetAllAsync(filter);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem chi tiết một CLB</summary>
    [HttpGet("{clubId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClubDetailDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid clubId)
    {
        var userId = TryGetUserId();
        var result = await _clubService.GetByIdAsync(clubId, userId);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("CLB không tồn tại."));
    }

    /// <summary>Xem các CLB đang tham gia</summary>
    [HttpGet("my-clubs")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClubSummaryDto>>), 200)]
    public async Task<IActionResult> GetMyClubs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _clubService.GetMyClubsAsync(GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Xem các CLB mình quản lý</summary>
    [HttpGet("managed")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClubSummaryDto>>), 200)]
    public async Task<IActionResult> GetManagedClubs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _clubService.GetManagedClubsAsync(GetUserId(), page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Cập nhật thông tin CLB</summary>
    [HttpPut("{clubId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateClub(Guid clubId, [FromBody] UpdateClubRequest request)
    {
        var result = await _clubService.UpdateClubAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] View statistics for a managed club</summary>
    [HttpGet("{clubId:guid}/statistics")]
    [Authorize]
    public async Task<IActionResult> GetClubStatistics(Guid clubId)
    {
        var result = await _statisticsService.GetClubStatisticsForUserAsync(clubId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Guid? TryGetUserId()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
