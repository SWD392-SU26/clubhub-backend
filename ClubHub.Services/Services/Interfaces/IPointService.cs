using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Point;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IPointService
{
    Task<MyPointSummaryDto?> GetMyPointsInClubAsync(Guid userId, Guid clubId);
    Task<PagedResult<MemberPointDto>> GetClubLeaderboardAsync(Guid clubId, int page, int pageSize);
    Task AddPointsAsync(Guid userId, Guid clubId, int points, PointType type, string? note, Guid? referenceId = null);
}
