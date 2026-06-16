using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
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

    public UniversityAdminController(IClubService clubService)
        => _clubService = clubService;

    /// <summary>Xem toàn bộ CLB (mọi trạng thái)</summary>
    [HttpGet("clubs")]
    public async Task<IActionResult> GetAllClubs([FromQuery] ClubFilterRequest filter)
    {
        var result = await _clubService.GetAllAsync(filter);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Tạo CLB trực tiếp (không qua hồ sơ)</summary>
    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClub([FromBody] CreateClubRequest request)
    {
        var adminId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _clubService.CreateClubAsync(request, adminId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAllClubs), ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Ẩn CLB</summary>
    [HttpPut("clubs/{clubId:guid}/hide")]
    public async Task<IActionResult> HideClub(Guid clubId)
    {
        var result = await _clubService.HideClubAsync(clubId);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Khóa CLB</summary>
    [HttpPut("clubs/{clubId:guid}/lock")]
    public async Task<IActionResult> LockClub(Guid clubId)
    {
        var result = await _clubService.LockClubAsync(clubId);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xóa mềm CLB</summary>
    [HttpDelete("clubs/{clubId:guid}")]
    public async Task<IActionResult> SoftDeleteClub(Guid clubId)
    {
        var result = await _clubService.DeleteClubAsync(clubId, hardDelete: false);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xóa cứng CLB</summary>
    [HttpDelete("clubs/{clubId:guid}/hard")]
    public async Task<IActionResult> HardDeleteClub(Guid clubId)
    {
        var result = await _clubService.DeleteClubAsync(clubId, hardDelete: true);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }
}
