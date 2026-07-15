using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Repositories;
using ClubHub.API.Enums;
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

    public async Task<ApiResult<PagedResult<AuditLogDto>>> GetClubAuditLogsAsync(
        Guid clubId, Guid requesterId, AuditLogFilterRequest filter)
    {
        var canView = await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == requesterId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

        if (!canView)
            return ApiResult<PagedResult<AuditLogDto>>.Failure(
                "Bạn không có quyền xem lịch sử hoạt động CLB này.");

        var logs = await QueryAuditLogsAsync(filter, clubId);
        return ApiResult<PagedResult<AuditLogDto>>.Success(logs);
    }

    public Task<PagedResult<AuditLogDto>> GetAllAuditLogsAsync(AuditLogFilterRequest filter)
        => QueryAuditLogsAsync(filter, null);

    private async Task<PagedResult<AuditLogDto>> QueryAuditLogsAsync(
        AuditLogFilterRequest filter, Guid? forcedClubId)
    {
        var query = _uow.AuditLogs.Query().AsNoTracking();

        if (forcedClubId.HasValue)
            query = query.Where(a => a.ClubId == forcedClubId.Value);
        else if (filter.ClubId.HasValue)
            query = query.Where(a => a.ClubId == filter.ClubId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action);
        if (filter.PerformedBy.HasValue)
            query = query.Where(a => a.PerformedBy == filter.PerformedBy.Value);
        if (filter.From.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityType, a.EntityId, a.Action,
                a.PerformedBy, a.PerformedByName, a.Details,
                a.Description, a.ClubId, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, filter.Page, filter.PageSize, total);
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
                a.PerformedBy, a.PerformedByName, a.Details,
                a.Description, a.ClubId, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
