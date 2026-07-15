using ClubHub.API.DTOs.Audit;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        Guid actorUserId,
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? clubId = null,
        Guid? targetUserId = null,
        string? details = null);

    Task<ApiResult<PagedResult<AuditLogDto>>> GetClubLogsAsync(
        Guid clubId,
        Guid requesterId,
        AuditLogFilterRequest filter);

    Task<PagedResult<AuditLogDto>> GetAllLogsAsync(AuditLogFilterRequest filter);
}
