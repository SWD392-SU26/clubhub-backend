using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/clubs/{clubId:guid}/members")]
[Authorize]
[Produces("application/json")]
public class MembershipController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipController(IMembershipService membershipService)
        => _membershipService = membershipService;

    /// <summary>[Student] Gửi đơn tham gia CLB</summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join(Guid clubId, [FromBody] JoinClubRequest request)
    {
        var result = await _membershipService.RequestJoinAsync(clubId, GetUserId(), request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data, "Đơn tham gia đã được gửi, chờ duyệt."))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Student] Rời khỏi CLB</summary>
    [HttpDelete("leave")]
    public async Task<IActionResult> Leave(Guid clubId)
    {
        var result = await _membershipService.LeaveClubAsync(clubId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Lấy danh sách thành viên</summary>
    [HttpGet]
    public async Task<IActionResult> GetMembers(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _membershipService.GetMembersAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Lấy danh sách đơn chờ duyệt</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _membershipService.GetPendingRequestsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Duyệt / từ chối đơn tham gia</summary>
    [HttpPut("requests/{membershipId:guid}/review")]
    public async Task<IActionResult> ReviewRequest(Guid clubId, Guid membershipId,
        [FromBody] ReviewMembershipRequest request)
    {
        var result = await _membershipService.ReviewRequestAsync(membershipId, GetUserId(), request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Gán vai trò cho thành viên</summary>
    [HttpPut("assign-role")]
    public async Task<IActionResult> AssignRole(Guid clubId, [FromBody] AssignRoleRequest request)
    {
        var result = await _membershipService.AssignRoleAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Xóa thành viên khỏi CLB</summary>
    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid clubId, Guid memberId)
    {
        var result = await _membershipService.RemoveMemberAsync(clubId, memberId, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[President] Chuyển quyền chủ nhiệm</summary>
    [HttpPut("transfer-admin")]
    public async Task<IActionResult> TransferAdmin(Guid clubId, [FromBody] TransferAdminRequest request)
    {
        var result = await _membershipService.TransferAdminAsync(clubId, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

/// <summary>Endpoint cho user xem lịch sử membership của chính mình</summary>
[ApiController]
[Route("api/my-memberships")]
[Authorize]
public class MyMembershipController : ControllerBase
{
    private readonly IMembershipService _membershipService;
    public MyMembershipController(IMembershipService s) => _membershipService = s;

    [HttpGet]
    public async Task<IActionResult> GetMyMemberships()
    {
        var result = await _membershipService.GetMyMembershipsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
