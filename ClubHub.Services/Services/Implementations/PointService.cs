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

    public PointService(IUnitOfWork uow) => _uow = uow;

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
}
