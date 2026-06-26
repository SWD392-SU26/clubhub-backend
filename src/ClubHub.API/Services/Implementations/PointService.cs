using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Point;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class PointService : IPointService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public PointService(AppDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    public async Task AddPointsAsync(Guid userId, Guid clubId, int points, PointType type, string? note, Guid? referenceId = null)
    {
        _db.PointTransactions.Add(new PointTransaction
        {
            UserId = userId,
            ClubId = clubId,
            Points = points,
            Type = type,
            Note = note,
            ReferenceId = referenceId
        });
        await _db.SaveChangesAsync();
    }

    public async Task<ApiResult<PointTransactionDto>> AddPointTransactionAsync(Guid clubId, CreatePointTransactionRequest request, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<PointTransactionDto>.Failure("You do not have permission to create point transactions.");

        var isMember = await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId &&
            m.UserId == request.UserId &&
            m.Status == MembershipStatus.Approved);

        if (!isMember)
            return ApiResult<PointTransactionDto>.Failure("Target user is not an approved member of this club.");

        var transaction = new PointTransaction
        {
            UserId = request.UserId,
            ClubId = clubId,
            Points = request.Points,
            Type = request.Type,
            Note = request.Note,
            ReferenceId = request.ReferenceId
        };

        _db.PointTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, "PointTransactionCreated", nameof(PointTransaction), transaction.Id, request.UserId, $"{request.Points:+#;-#;0} points: {request.Type}.");

        return ApiResult<PointTransactionDto>.Success(MapToDto(transaction));
    }

    public async Task<MyPointSummaryDto?> GetMyPointsInClubAsync(Guid userId, Guid clubId)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return null;

        var transactions = await _db.PointTransactions
            .Where(pt => pt.UserId == userId && pt.ClubId == clubId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();

        var totalPoints = transactions.Sum(t => t.Points);

        var allMemberPoints = await _db.PointTransactions
            .Where(pt => pt.ClubId == clubId)
            .GroupBy(pt => pt.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(t => t.Points) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        var rank = allMemberPoints.FindIndex(x => x.UserId == userId) + 1;
        if (rank == 0) rank = allMemberPoints.Count + 1;

        var recent = transactions.Take(10).Select(MapToDto).ToList();

        return new MyPointSummaryDto(clubId, club.Name, totalPoints, rank, recent);
    }

    public async Task<PagedResult<MemberPointDto>> GetClubLeaderboardAsync(Guid clubId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var allMemberPoints = await _db.PointTransactions
            .Where(pt => pt.ClubId == clubId)
            .GroupBy(pt => pt.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(t => t.Points) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        var total = allMemberPoints.Count;
        var paged = allMemberPoints
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var userIds = paged.Select(x => x.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var items = paged.Select((x, i) => new MemberPointDto(
            x.UserId,
            users.TryGetValue(x.UserId, out var u) ? u.FullName : "Unknown",
            users.TryGetValue(x.UserId, out var u2) ? u2.AvatarUrl : null,
            x.Total,
            (page - 1) * pageSize + i + 1
        )).ToList();

        return new PagedResult<MemberPointDto>(items, page, pageSize, total);
    }

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private static PointTransactionDto MapToDto(PointTransaction transaction) => new(
        transaction.Id,
        transaction.Points,
        transaction.Type.ToString(),
        transaction.Note,
        transaction.CreatedAt);
}
