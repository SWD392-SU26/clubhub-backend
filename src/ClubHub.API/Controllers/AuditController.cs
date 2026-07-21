using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly IMembershipService _membershipService;

    public AuditController(IAuditService auditService, IMembershipService membershipService)
    {
        _auditService = auditService;
        _membershipService = membershipService;
    }

    /// <summary>[Club Admin] Xem lịch sử hoạt động của CLB (chỉ admin của CLB đó mới xem được)</summary>
    [HttpGet("api/clubs/{clubId:guid}/audit-logs")]
    [Authorize(Roles = "ClubAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetClubAuditLogs(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _membershipService.IsClubAdminAsync(clubId, GetUserId()))
            return Forbid();

        var result = await _auditService.GetClubAuditLogsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[University Admin] Xem lịch sử hoạt động của một entity cụ thể</summary>
    [HttpGet("api/audit-logs/{entityType}/{entityId:guid}")]
    [Authorize(Roles = "UniversityAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
    public async Task<IActionResult> GetEntityAuditLogs(string entityType, Guid entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetEntityAuditLogsAsync(entityType, entityId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
