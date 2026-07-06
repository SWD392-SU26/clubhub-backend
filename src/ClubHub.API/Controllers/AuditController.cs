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

    public AuditController(IAuditService auditService) => _auditService = auditService;

    /// <summary>[Club Admin] Xem lịch sử hoạt động của CLB</summary>
    [HttpGet("api/clubs/{clubId:guid}/audit-logs")]
    [Authorize]
    public async Task<IActionResult> GetClubAuditLogs(Guid clubId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetClubAuditLogsAsync(clubId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem lịch sử hoạt động của một entity cụ thể</summary>
    [HttpGet("api/audit-logs/{entityType}/{entityId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetEntityAuditLogs(string entityType, Guid entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetEntityAuditLogsAsync(entityType, entityId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
