using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IClubService
{
    Task<PagedResult<ClubSummaryDto>> GetAllAsync(ClubFilterRequest filter);
    Task<ClubDetailDto?> GetByIdAsync(Guid clubId, Guid? currentUserId = null);
    Task<ApiResult<ClubDetailDto>> CreateClubAsync(CreateClubRequest request, Guid createdBy);
    Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest request, Guid requesterId);
    Task<ApiResult<bool>> HideClubAsync(Guid clubId);
    Task<ApiResult<bool>> LockClubAsync(Guid clubId);
    Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, bool hardDelete = false);
    Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize);
}
