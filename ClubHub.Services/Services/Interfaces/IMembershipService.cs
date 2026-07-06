using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IMembershipService
{
    Task<ApiResult<bool>> RequestJoinAsync(Guid clubId, Guid userId, JoinClubRequest request);
    Task<ApiResult<bool>> ReviewRequestAsync(Guid membershipId, Guid reviewerId, ReviewMembershipRequest request);
    Task<ApiResult<bool>> LeaveClubAsync(Guid clubId, Guid userId);
    Task<ApiResult<bool>> RemoveMemberAsync(Guid clubId, Guid memberId, Guid requesterId);
    Task<ApiResult<bool>> AssignRoleAsync(Guid clubId, AssignRoleRequest request, Guid requesterId);
    Task<ApiResult<bool>> TransferAdminAsync(Guid clubId, TransferAdminRequest request, Guid currentAdminId);
    Task<PagedResult<MembershipRequestDto>> GetPendingRequestsAsync(Guid clubId, int page, int pageSize);
    Task<PagedResult<ClubMemberDto>> GetMembersAsync(Guid clubId, int page, int pageSize);
    Task<List<MyMembershipDto>> GetMyMembershipsAsync(Guid userId);
}
