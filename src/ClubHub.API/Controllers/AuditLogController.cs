using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/clubs/{clubId:guid}")]
[Authorize]
[Produces("application/json")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
        => _auditLogService = auditLogService;

    /// <summary>[Club Admin] View club activity history</summary>
    [HttpGet("activity-history")]
    public async Task<IActionResult> GetClubActivityHistory(Guid clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogService.GetClubAuditLogsAsync(clubId, GetUserId(), page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Club Admin] View member audit log</summary>
    [HttpGet("members/{memberUserId:guid}/audit-log")]
    public async Task<IActionResult> GetMemberAuditLog(Guid clubId, Guid memberUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogService.GetMemberAuditLogsAsync(clubId, memberUserId, GetUserId(), page, pageSize);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
