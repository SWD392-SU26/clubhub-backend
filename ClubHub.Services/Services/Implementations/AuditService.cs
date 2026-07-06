using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;

    public AuditService(IUnitOfWork uow) => _uow = uow;

    public async Task LogAsync(string entityType, Guid entityId, string action, Guid? performedBy,
        string? performedByName, Guid? clubId, string? details = null, string? description = null)
    {
        // Auto-resolve performer name if not provided
        if (string.IsNullOrEmpty(performedByName) && performedBy.HasValue)
        {
            var user = await _uow.Users.GetByIdAsync(performedBy.Value);
            performedByName = user?.FullName;
        }

        _uow.AuditLogs.Add(new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            ClubId = clubId,
            Details = details,
            Description = description
        });
        await _uow.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetClubAuditLogsAsync(Guid clubId, int page, int pageSize)
    {
        var query = _uow.AuditLogs.Query()
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Action,
                a.PerformedByName, a.Description, a.ClubId, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AuditLogDto>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int page, int pageSize)
    {
        var query = _uow.AuditLogs.Query()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Action,
                a.PerformedByName, a.Description, a.ClubId, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
