using ClubHub.API.DTOs.Audit;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid clubId, Guid? actorUserId, string action, string? targetType = null, Guid? targetId = null, Guid? targetUserId = null, string? description = null);
    Task<ApiResult<PagedResult<AuditLogDto>>> GetClubAuditLogsAsync(Guid clubId, Guid requesterId, int page, int pageSize);
    Task<ApiResult<PagedResult<AuditLogDto>>> GetMemberAuditLogsAsync(Guid clubId, Guid memberUserId, Guid requesterId, int page, int pageSize);
}
