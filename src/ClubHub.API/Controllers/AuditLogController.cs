using System.Security.Claims;
using ClubHub.API.DTOs.Audit;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
        => _auditLogService = auditLogService;

    [HttpGet("api/clubs/{clubId:guid}/audit-logs")]
    public async Task<IActionResult> GetClubLogs(Guid clubId, [FromQuery] AuditLogFilterRequest filter)
    {
        var result = await _auditLogService.GetClubLogsAsync(clubId, GetUserId(), filter);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(result.Error!));
    }

    [HttpGet("api/admin/audit-logs")]
    [Authorize(Roles = "UniversityAdmin")]
    public async Task<IActionResult> GetAllLogs([FromQuery] AuditLogFilterRequest filter)
    {
        var result = await _auditLogService.GetAllLogsAsync(filter);
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
