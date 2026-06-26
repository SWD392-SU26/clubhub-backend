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
    [HttpGet("ranking")]
    public async Task<IActionResult> GetLeaderboard(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _pointService.GetClubLeaderboardAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Create a point transaction for a club member</summary>
    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction(Guid clubId, [FromBody] CreatePointTransactionRequest request)
    {
        var result = await _pointService.AddPointTransactionAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
