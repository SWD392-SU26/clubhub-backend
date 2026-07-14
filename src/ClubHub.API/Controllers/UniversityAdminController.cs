using System.Security.Claims;
using ClubHub.API.DTOs.Auth;
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
    private readonly IUserManagementService _userManagementService;

    public UniversityAdminController(IClubService clubService, IUserManagementService userManagementService)
    {
        _clubService = clubService;
        _userManagementService = userManagementService;
    }

    // ── User Management ───────────────────────────────────────────────────────

    /// <summary>Xem danh sách users (phân trang, filter role, loại trừ UniversityAdmin)</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] Role? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userManagementService.GetUsersAsync(role, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Cập nhật trạng thái tài khoản (Active/Inactive/Lock/Deleted)</summary>
    [HttpPut("users/{userId:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequest request)
    {
        var result = await _userManagementService.UpdateUserStatusAsync(userId, request.Status);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Lấy danh sách ClubAdmin để chọn khi tạo CLB</summary>
    [HttpGet("club-admins")]
    public async Task<IActionResult> GetClubAdmins()
    {
        var result = await _clubService.GetClubAdminsAsync();
        return Ok(ApiResponse.Ok(result));
    }

    // ── Club Management ───────────────────────────────────────────────────────

    /// <summary>Xem toàn bộ CLB (lọc theo trạng thái)</summary>
    [HttpGet("clubs")]
    public async Task<IActionResult> GetAllClubs(
        [FromQuery] ClubStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clubService.GetAllByStatusAsync(status, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Tạo CLB trực tiếp (không qua hồ sơ, chọn ClubAdmin từ danh sách)</summary>
    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClubWithAdmin([FromBody] CreateClubWithAdminRequest request)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _clubService.CreateClubWithAdminAsync(request, adminId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAllClubs), ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Cập nhật trạng thái CLB (Active/Inactive/Lock/Deleted)</summary>
    [HttpPut("clubs/{clubId:guid}/status")]
    public async Task<IActionResult> UpdateClubStatus(Guid clubId, [FromBody] UpdateClubStatusRequest request)
    {
        var result = await _clubService.UpdateStatusAsync(clubId, request.Status);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Ẩn CLB (Inactive)</summary>
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

    /// <summary>Mở lại CLB</summary>
    [HttpPut("clubs/{clubId:guid}/reopen")]
    public async Task<IActionResult> ReopenClub(Guid clubId)
    {
        var result = await _clubService.ReopenClubAsync(clubId);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Giải tán CLB (Deleted)</summary>
    [HttpPut("clubs/{clubId:guid}/dissolve")]
    public async Task<IActionResult> DissolveClub(Guid clubId)
    {
        var result = await _clubService.DissolveClubAsync(clubId);
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