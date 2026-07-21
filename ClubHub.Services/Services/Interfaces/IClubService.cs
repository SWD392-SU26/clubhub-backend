using ClubHub.API.DTOs.Auth;
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
    Task<ApiResult<ClubDetailDto>> CreateClubWithAdminAsync(CreateClubWithAdminRequest request, Guid createdByUniAdmin);
    Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest request, Guid requesterId);
    Task<ApiResult<bool>> HideClubAsync(Guid clubId);
    Task<ApiResult<bool>> LockClubAsync(Guid clubId);
    Task<ApiResult<bool>> ArchiveClubAsync(Guid clubId);
    Task<ApiResult<bool>> ReopenClubAsync(Guid clubId);
    Task<ApiResult<bool>> DissolveClubAsync(Guid clubId);
    Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, bool hardDelete = false);
    Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize);
    Task<PagedResult<ClubSummaryDto>> GetAllByStatusAsync(ClubStatus? status, ClubCategory? clubcategories, int page, int pageSize);
    /// <summary>Lấy danh sách ClubAdmin để UniAdmin chọn khi tạo CLB</summary>
    Task<List<UserProfileDto>> GetClubAdminsAsync();
    Task<ApiResult<bool>> UpdateStatusAsync(Guid clubId, ClubStatus status);
}
