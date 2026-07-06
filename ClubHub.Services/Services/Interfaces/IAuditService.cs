using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;

namespace ClubHub.API.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityType, Guid entityId, string action, Guid? performedBy,
        string? performedByName, Guid? clubId, string? details = null, string? description = null);

    Task<PagedResult<AuditLogDto>> GetClubAuditLogsAsync(Guid clubId, int page, int pageSize);
    Task<PagedResult<AuditLogDto>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int page, int pageSize);
}

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? PerformedByName,
    string? Description,
    Guid? ClubId,
    DateTime CreatedAt
);
