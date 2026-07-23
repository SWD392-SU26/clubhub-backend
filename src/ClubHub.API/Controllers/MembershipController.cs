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

    public MembershipController(IMembershipService membershipService) => _membershipService = membershipService;

    /// <summary>[Student] Gửi đơn tham gia CLB</summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Join(Guid clubId, [FromBody] JoinClubRequest request)
    {
        var result = await _membershipService.RequestJoinAsync(clubId, GetUserId(), request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data, "Đơn tham gia đã được gửi, chờ duyệt."))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Student] Rút đơn tham gia CLB (khi đơn còn pending)</summary>
    [HttpDelete("cancel-request")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CancelJoinRequest(Guid clubId)
    {
        var result = await _membershipService.CancelJoinRequestAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Student] Rời khỏi CLB</summary>
    [HttpDelete("leave")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Leave(Guid clubId)
    {
        var result = await _membershipService.LeaveClubAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Lấy danh sách thành viên</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClubMemberDto>>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMembers(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _membershipService.IsClubAdminAsync(clubId, GetUserId()))
            return Forbid();

        var result = await _membershipService.GetMembersAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Lấy danh sách đơn chờ duyệt</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MembershipRequestDto>>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetPendingRequests(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _membershipService.IsClubAdminAsync(clubId, GetUserId()))
            return Forbid();

        var result = await _membershipService.GetPendingRequestsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[Club Admin] Duyệt / từ chối đơn tham gia</summary>
    [HttpPut("requests/{membershipId:guid}/review")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ReviewRequest(Guid clubId, Guid membershipId,
        [FromBody] ReviewMembershipRequest request)
    {
        var result = await _membershipService.ReviewRequestAsync(membershipId, GetUserId(), request);
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Gán vai trò cho thành viên</summary>
    [HttpPut("assign-role")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> AssignRole(Guid clubId, [FromBody] AssignRoleRequest request)
    {
        var result = await _membershipService.AssignRoleAsync(clubId, request, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] Xóa thành viên khỏi CLB</summary>
    [HttpDelete("{memberId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RemoveMember(Guid clubId, Guid memberId)
    {
        var result = await _membershipService.RemoveMemberAsync(clubId, memberId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[President] Chuyển quyền chủ nhiệm</summary>
    [HttpPut("transfer-admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> TransferAdmin(Guid clubId, [FromBody] TransferAdminRequest request)
    {
        var result = await _membershipService.TransferAdminAsync(clubId, request, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[President] Đề cử người kế nhiệm (khi muốn rời CLB)</summary>
    [HttpPut("nominate-successor")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> NominateSuccessor(Guid clubId, [FromBody] TransferAdminRequest request)
    {
        var result = await _membershipService.NominateSuccessorAsync(clubId, request.NewAdminUserId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Member] Chấp nhận kế nhiệm chủ nhiệm</summary>
    [HttpPut("accept-succession")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> AcceptSuccession(Guid clubId)
    {
        var result = await _membershipService.AcceptSuccessionAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Member] Từ chối kế nhiệm chủ nhiệm</summary>
    [HttpPut("reject-succession")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RejectSuccession(Guid clubId)
    {
        var result = await _membershipService.RejectSuccessionAsync(clubId, GetUserId());
        return result.IsSuccess ? Ok(ApiResponse.Ok(result.Data)) : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}    /// <summary>Endpoint cho user xem lịch sử membership của chính mình</summary>
[ApiController]
[Route("api/my-memberships")]
[Authorize]
[Produces("application/json")]
public class MyMembershipController : ControllerBase
{
    private readonly IMembershipService _membershipService;
    public MyMembershipController(IMembershipService s) => _membershipService = s;

    /// <summary>Xem danh sách CLB đã/đang tham gia</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MyMembershipDto>>), 200)]
    public async Task<IActionResult> GetMyMemberships()
    {
        var result = await _membershipService.GetMyMembershipsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}