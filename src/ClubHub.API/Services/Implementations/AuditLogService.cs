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

    public async Task LogAsync(Guid clubId, Guid? actorUserId, string action, string? targetType = null, Guid? targetId = null, Guid? targetUserId = null, string? description = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ClubId = clubId,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Description = description
        });

        await _db.SaveChangesAsync();
    }

    public async Task<ApiResult<PagedResult<AuditLogDto>>> GetClubAuditLogsAsync(Guid clubId, Guid requesterId, int page, int pageSize)
    {
        if (!await IsClubManagerAsync(clubId, requesterId))
            return ApiResult<PagedResult<AuditLogDto>>.Failure("You do not have permission to view club activity history.");

        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .Include(a => a.TargetUser)
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return ApiResult<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>(items, page, pageSize, total));
    }

    public async Task<ApiResult<PagedResult<AuditLogDto>>> GetMemberAuditLogsAsync(Guid clubId, Guid memberUserId, Guid requesterId, int page, int pageSize)
    {
        if (!await IsClubManagerAsync(clubId, requesterId))
            return ApiResult<PagedResult<AuditLogDto>>.Failure("You do not have permission to view member audit logs.");

        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .Include(a => a.TargetUser)
            .Where(a => a.ClubId == clubId && (a.TargetUserId == memberUserId || a.ActorUserId == memberUserId))
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return ApiResult<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>(items, page, pageSize, total));
    }

    private async Task<bool> IsClubManagerAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private static AuditLogDto MapToDto(AuditLog a) => new(
        a.Id,
        a.ClubId,
        a.ActorUserId,
        a.ActorUser?.FullName,
        a.TargetUserId,
        a.TargetUser?.FullName,
        a.Action,
        a.TargetType,
        a.TargetId,
        a.Description,
        a.CreatedAt);
}
