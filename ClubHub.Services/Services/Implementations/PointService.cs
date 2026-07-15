using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Point;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class PointService : IPointService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public PointService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task AddPointsAsync(Guid userId, Guid clubId, int points, PointType type, string? note, Guid? referenceId = null)
    {
        _uow.PointTransactions.Add(new PointTransaction
        {
            UserId = userId,
            ClubId = clubId,
            Points = points,
            Type = type,
            Note = note,
            ReferenceId = referenceId
        });
        await _uow.SaveChangesAsync();
    }

    public async Task<MyPointSummaryDto?> GetMyPointsInClubAsync(Guid userId, Guid clubId)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null) return null;

        var transactions = await _uow.PointTransactions.Query()
            .Where(pt => pt.UserId == userId && pt.ClubId == clubId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();

        var totalPoints = transactions.Sum(t => t.Points);

        // Calculate rank
        var allMemberPoints = await _uow.PointTransactions.Query()
            .Where(pt => pt.ClubId == clubId)
            .GroupBy(pt => pt.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(t => t.Points) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        var rank = allMemberPoints.FindIndex(x => x.UserId == userId) + 1;

        var recent = transactions.Take(10).Select(t => new PointTransactionDto(
            t.Id, t.Points, t.Type.ToString(), t.Note, t.CreatedAt)).ToList();

        return new MyPointSummaryDto(clubId, club.Name, totalPoints, rank, recent);
    }

    public async Task<PagedResult<MemberPointDto>> GetClubLeaderboardAsync(Guid clubId, int page, int pageSize)
    {
        var allMemberPoints = await _uow.PointTransactions.Query()
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
        var users = await _uow.Users.Query()
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

    public async Task<ApiResult<PointHistoryDto>> AdjustPointsAsync(
        Guid clubId, Guid requesterId, AdjustPointsRequest request)
    {
        if (!await _uow.ClubMembers.IsClubAdminAsync(clubId, requesterId))
            return ApiResult<PointHistoryDto>.Failure("Bạn không có quyền điều chỉnh điểm của CLB này.");

        var member = await _uow.ClubMembers.QueryApprovedMembers(clubId)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId);
        if (member == null)
            return ApiResult<PointHistoryDto>.Failure(
                "Người nhận điểm không phải thành viên đang hoạt động của CLB.");

        var resolvedPoints = ResolvePoints(request);
        if (!resolvedPoints.IsSuccess)
            return ApiResult<PointHistoryDto>.Failure(resolvedPoints.Error!);

        var transaction = new PointTransaction
        {
            UserId = request.UserId,
            ClubId = clubId,
            Points = resolvedPoints.Data,
            Type = request.Type,
            Note = request.Note,
            ReferenceId = request.ReferenceId,
            AdjustedByUserId = requesterId
        };

        _uow.PointTransactions.Add(transaction);
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync(
            "PointTransaction",
            transaction.Id,
            "AdjustPoints",
            requesterId,
            null,
            clubId,
            $"{{\"targetUserId\":\"{request.UserId}\",\"type\":\"{request.Type}\",\"points\":{transaction.Points}}}",
            $"Điều chỉnh {transaction.Points:+#;-#;0} điểm cho {member.User.FullName}");

        var requesterName = await _uow.Users.Query()
            .Where(u => u.Id == requesterId)
            .Select(u => u.FullName)
            .FirstAsync();

        return ApiResult<PointHistoryDto>.Success(new PointHistoryDto(
            transaction.Id,
            member.UserId,
            member.User.FullName,
            member.User.StudentCode,
            transaction.Points,
            transaction.Type.ToString(),
            transaction.Note,
            transaction.ReferenceId,
            requesterId,
            requesterName,
            transaction.CreatedAt));
    }

    public async Task<ApiResult<PagedResult<PointHistoryDto>>> GetPointHistoryAsync(
        Guid clubId, Guid requesterId, PointHistoryFilterRequest filter)
    {
        if (!await _uow.ClubMembers.IsClubAdminAsync(clubId, requesterId))
            return ApiResult<PagedResult<PointHistoryDto>>.Failure(
                "Bạn không có quyền xem lịch sử điểm của CLB này.");

        var query = _uow.PointTransactions.Query()
            .AsNoTracking()
            .Where(t => t.ClubId == clubId);

        if (filter.UserId.HasValue) query = query.Where(t => t.UserId == filter.UserId.Value);
        if (filter.Type.HasValue) query = query.Where(t => t.Type == filter.Type.Value);
        if (filter.From.HasValue) query = query.Where(t => t.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(t => t.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new PointHistoryDto(
                t.Id,
                t.UserId,
                t.User.FullName,
                t.User.StudentCode,
                t.Points,
                t.Type.ToString(),
                t.Note,
                t.ReferenceId,
                t.AdjustedByUserId,
                t.AdjustedByUser != null ? t.AdjustedByUser.FullName : null,
                t.CreatedAt))
            .ToListAsync();

        return ApiResult<PagedResult<PointHistoryDto>>.Success(
            new PagedResult<PointHistoryDto>(items, filter.Page, filter.PageSize, total));
    }

    private static ApiResult<int> ResolvePoints(AdjustPointsRequest request)
    {
        var fixedPoints = request.Type switch
        {
            PointType.Activity => 5,
            PointType.Support => 15,
            PointType.Absence => -5,
            _ => (int?)null
        };

        if (fixedPoints.HasValue)
        {
            if (request.Points.HasValue && request.Points.Value != fixedPoints.Value)
                return ApiResult<int>.Failure(
                    $"Loại {request.Type} phải có số điểm là {fixedPoints.Value}.");
            return ApiResult<int>.Success(fixedPoints.Value);
        }

        if (request.Type == PointType.Bonus && request.Points is > 0)
            return ApiResult<int>.Success(request.Points.Value);
        if (request.Type == PointType.Penalty && request.Points is < 0)
            return ApiResult<int>.Success(request.Points.Value);

        return ApiResult<int>.Failure(
            "Chỉ có thể điều chỉnh Activity, Support, Absence, Bonus (điểm dương) hoặc Penalty (điểm âm).");
    }
}
