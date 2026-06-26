using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IClubService
{
    Task<PagedResult<ClubSummaryDto>> GetAllAsync(ClubFilterRequest filter);
    Task<PagedResult<ClubSummaryDto>> GetAdminClubsAsync(ClubFilterRequest filter);
    Task<ClubDetailDto?> GetByIdAsync(Guid clubId, Guid? currentUserId = null);
    Task<ApiResult<ClubDetailDto>> CreateClubAsync(CreateClubRequest request, Guid createdBy);
    Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest request, Guid requesterId);
    Task<ApiResult<bool>> HideClubAsync(Guid clubId, Guid requesterId);
    Task<ApiResult<bool>> LockClubAsync(Guid clubId, Guid requesterId);
    Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, Guid requesterId, bool hardDelete = false);
    Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize);
    Task<PagedResult<ClubSummaryDto>> GetManagedClubsAsync(Guid userId, int page, int pageSize);
}
