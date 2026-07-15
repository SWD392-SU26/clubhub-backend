using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Point;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/clubs/{clubId:guid}/points")]
[Authorize]
[Produces("application/json")]
public class PointController : ControllerBase
{
    private readonly IPointService _pointService;

    public PointController(IPointService pointService) => _pointService = pointService;

    /// <summary>[Club Member] Xem điểm thi đua của mình trong CLB</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyPoints(Guid clubId)
    {
        var result = await _pointService.GetMyPointsInClubAsync(GetUserId(), clubId);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("CLB không tồn tại."));
    }

    /// <summary>Xem bảng xếp hạng điểm thi đua của CLB</summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _pointService.GetClubLeaderboardAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Cộng/trừ điểm thủ công theo quy tắc của CLB</summary>
    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustPoints(Guid clubId, [FromBody] AdjustPointsRequest request)
    {
        var result = await _pointService.AdjustPointsAsync(clubId, GetUserId(), request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Xem lịch sử cộng/trừ điểm của CLB</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPointHistory(Guid clubId, [FromQuery] PointHistoryFilterRequest filter)
    {
        var result = await _pointService.GetPointHistoryAsync(clubId, GetUserId(), filter);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
