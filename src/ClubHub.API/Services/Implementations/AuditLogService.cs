using ClubHub.API.Data;
using ClubHub.API.DTOs.Audit;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db) => _db = db;

    public async Task LogAsync(
        Guid actorUserId,
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? clubId = null,
        Guid? targetUserId = null,
        string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ClubId = clubId,
            TargetUserId = targetUserId,
            Details = details is { Length: > 2000 } ? details[..2000] : details
        });
        await _db.SaveChangesAsync();
    }

    public async Task<ApiResult<PagedResult<AuditLogDto>>> GetClubLogsAsync(
        Guid clubId,
        Guid requesterId,
        AuditLogFilterRequest filter)
    {
        var canView = await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == requesterId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

        if (!canView)
            return ApiResult<PagedResult<AuditLogDto>>.Failure("Bạn không có quyền xem lịch sử hoạt động CLB này.");

        var logs = await Query(filter, clubId);
        return ApiResult<PagedResult<AuditLogDto>>.Success(logs);
    }

    public Task<PagedResult<AuditLogDto>> GetAllLogsAsync(AuditLogFilterRequest filter)
        => Query(filter, null);

    private async Task<PagedResult<AuditLogDto>> Query(AuditLogFilterRequest filter, Guid? clubId)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (clubId.HasValue)
            query = query.Where(l => l.ClubId == clubId.Value);
        else if (filter.ClubId.HasValue)
            query = query.Where(l => l.ClubId == filter.ClubId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(l => l.Action == filter.Action);
        if (filter.ActorUserId.HasValue) query = query.Where(l => l.ActorUserId == filter.ActorUserId);
        if (filter.TargetUserId.HasValue) query = query.Where(l => l.TargetUserId == filter.TargetUserId);
        if (filter.From.HasValue) query = query.Where(l => l.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(l => l.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.ClubId,
                l.Club != null ? l.Club.Name : null,
                l.ActorUserId,
                l.Actor.FullName,
                l.TargetUserId,
                l.TargetUser != null ? l.TargetUser.FullName : null,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Details,
                l.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, filter.Page, filter.PageSize, total);
    }
}
