using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class MembershipService : IMembershipService
{
    private readonly IUnitOfWork _uow;

    public MembershipService(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResult<bool>> RequestJoinAsync(Guid clubId, Guid userId, JoinClubRequest req)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null || club.Status != ClubStatus.Active)
            return ApiResult<bool>.Failure("CLB không tồn tại hoặc không hoạt động.");

        var existing = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);

        if (existing != null)
        {
            if (existing.Status == MembershipStatus.Pending)
                return ApiResult<bool>.Failure("Bạn đã gửi đơn tham gia CLB này rồi.");
            if (existing.Status == MembershipStatus.Approved)
                return ApiResult<bool>.Failure("Bạn đã là thành viên của CLB này.");
        }

        var membership = new ClubMember
        {
            UserId = userId,
            ClubId = clubId,
            JoinReason = req.JoinReason,
            Status = MembershipStatus.Pending
        };

        _uow.ClubMembers.Add(membership);
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> ReviewRequestAsync(Guid membershipId, Guid reviewerId, ReviewMembershipRequest req)
    {
        var membership = await _uow.ClubMembers.GetByIdAsync(membershipId);
        if (membership == null) return ApiResult<bool>.Failure("Đơn không tồn tại.");
        if (membership.Status != MembershipStatus.Pending)
            return ApiResult<bool>.Failure("Đơn này đã được xử lý.");

        if (!await IsClubAdminAsync(membership.ClubId, reviewerId))
            return ApiResult<bool>.Failure("Bạn không có quyền duyệt đơn này.");

        membership.Status = req.IsApproved ? MembershipStatus.Approved : MembershipStatus.Rejected;
        membership.ReviewedBy = reviewerId;
        membership.ReviewedAt = DateTime.UtcNow;

        if (req.IsApproved)
            membership.JoinedAt = DateTime.UtcNow;
        else
            membership.RejectionReason = req.RejectionReason;

        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> LeaveClubAsync(Guid clubId, Guid userId)
    {
        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);
        if (membership == null || membership.Status != MembershipStatus.Approved) return ApiResult<bool>.Failure("Bạn không phải thành viên CLB này.");

        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("Chủ nhiệm phải chuyển quyền trước khi rời CLB.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RemoveMemberAsync(Guid clubId, Guid memberId, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa thành viên.");

        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, memberId);
        if (membership == null || membership.Status != MembershipStatus.Approved) return ApiResult<bool>.Failure("Thành viên không tồn tại.");
        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("Không thể xóa chủ nhiệm CLB.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AssignRoleAsync(Guid clubId, AssignRoleRequest req, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền gán vai trò.");

        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.UserId);
        if (membership == null || membership.Status != MembershipStatus.Approved) return ApiResult<bool>.Failure("Thành viên không tồn tại.");

        membership.RoleInClub = req.NewRole;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> TransferAdminAsync(Guid clubId, TransferAdminRequest req, Guid currentAdminId)
    {
        var currentAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, currentAdminId);
        if (currentAdmin == null || currentAdmin.Status != MembershipStatus.Approved || currentAdmin.RoleInClub != ClubRole.President)
            return ApiResult<bool>.Failure("Bạn không phải chủ nhiệm CLB.");

        var newAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.NewAdminUserId);
        if (newAdmin == null || newAdmin.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Người nhận quyền không phải thành viên CLB.");

        currentAdmin.RoleInClub = ClubRole.Member;
        newAdmin.RoleInClub = ClubRole.President;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<MembershipRequestDto>> GetPendingRequestsAsync(Guid clubId, int page, int pageSize)
    {
        var query = _uow.ClubMembers.QueryPendingRequests(clubId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MembershipRequestDto(
                m.Id, m.UserId, m.User.FullName, m.User.AvatarUrl,
                m.User.StudentCode, m.JoinReason ?? "", m.Status.ToString(), m.RequestedAt))
            .ToListAsync();

        return new PagedResult<MembershipRequestDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<ClubMemberDto>> GetMembersAsync(Guid clubId, int page, int pageSize)
    {
        var query = _uow.ClubMembers.QueryApprovedMembers(clubId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ClubMemberDto(
                m.Id, m.UserId, m.User.FullName, m.User.AvatarUrl,
                m.User.StudentCode, m.RoleInClub.ToString(), m.JoinedAt ?? DateTime.UtcNow))
            .ToListAsync();

        return new PagedResult<ClubMemberDto>(items, page, pageSize, total);
    }

    public async Task<List<MyMembershipDto>> GetMyMembershipsAsync(Guid userId)
    {
        return await _uow.ClubMembers.QueryMyMemberships(userId)
            .Select(m => new MyMembershipDto(
                m.ClubId, m.Club.Name, m.Club.LogoUrl,
                m.RoleInClub.ToString(), m.Status.ToString(),
                m.RequestedAt, m.JoinedAt))
            .ToListAsync();
    }

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));
}
