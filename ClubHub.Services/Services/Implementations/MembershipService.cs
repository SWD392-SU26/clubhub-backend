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
    private readonly INotificationService _notificationService;

    public MembershipService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

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

    public async Task<ApiResult<bool>> CancelJoinRequestAsync(Guid clubId, Guid userId)
    {
        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);
        if (membership == null || membership.Status != MembershipStatus.Pending)
            return ApiResult<bool>.Failure("Không tìm thấy đơn tham gia đang chờ xử lý.");

        membership.Status = MembershipStatus.Cancelled;
        membership.ReviewedAt = DateTime.UtcNow;
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

        var club = await _uow.Clubs.GetByIdAsync(membership.ClubId);
        var clubName = club?.Name ?? "CLB";

        if (req.IsApproved)
        {
            membership.JoinedAt = DateTime.UtcNow;
            await _notificationService.SendNotificationAsync(
                membership.UserId,
                "Đơn tham gia CLB được duyệt",
                $"Chúc mừng! Bạn đã được duyệt vào CLB {clubName}.",
                "JOIN_APPROVED");
        }
        else
        {
            membership.RejectionReason = req.RejectionReason;
            await _notificationService.SendNotificationAsync(
                membership.UserId,
                "Đơn tham gia CLB bị từ chối",
                $"Đơn tham gia CLB {clubName} của bạn đã bị từ chối. " +
                $"Lý do: {req.RejectionReason ?? "Không rõ"}",
                "JOIN_REJECTED");
        }

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

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _notificationService.SendNotificationAsync(
            memberId,
            "Bị xóa khỏi CLB",
            $"Bạn đã bị xóa khỏi CLB {club?.Name ?? ""}.",
            "MEMBER_REMOVED");

        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AssignRoleAsync(Guid clubId, AssignRoleRequest req, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền gán vai trò.");

        // Only the current President can assign a new President
        if (req.NewRole == ClubRole.President && !await IsPresidentAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Chỉ Chủ nhiệm mới có thể bổ nhiệm Chủ nhiệm mới.");

        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.UserId);
        if (membership == null || membership.Status != MembershipStatus.Approved) return ApiResult<bool>.Failure("Thành viên không tồn tại.");

        var club = await _uow.Clubs.GetByIdAsync(clubId);

        // If assigning to President, demote the current President first
        if (req.NewRole == ClubRole.President)
        {
            var currentPresident = await _uow.ClubMembers.FirstOrDefaultAsync(m =>
                m.ClubId == clubId && m.RoleInClub == ClubRole.President && m.Status == MembershipStatus.Approved);

            if (currentPresident != null && currentPresident.UserId != req.UserId)
            {
                currentPresident.RoleInClub = ClubRole.Member;

                await _notificationService.SendNotificationAsync(
                    currentPresident.UserId,
                    "Vai trò trong CLB thay đổi",
                    $"Bạn không còn là Chủ nhiệm CLB {club?.Name ?? ""}. Vai trò của bạn đã được cập nhật.",
                    "ROLE_CHANGED");
            }
        }

        membership.RoleInClub = req.NewRole;

        await _notificationService.SendNotificationAsync(
            req.UserId,
            "Vai trò trong CLB thay đổi",
            $"Vai trò của bạn trong CLB {club?.Name ?? ""} đã được cập nhật thành: {DisplayRole(req.NewRole)}.",
            "ROLE_CHANGED");

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

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _notificationService.SendNotificationAsync(
            req.NewAdminUserId,
            "Chuyển quyền chủ nhiệm",
            $"Bạn đã được bổ nhiệm làm Chủ nhiệm CLB {club?.Name ?? ""}.",
            "TRANSFER_ADMIN");

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

    private static string DisplayRole(ClubRole role) => role switch
    {
        ClubRole.President => "Chủ nhiệm",
        ClubRole.VicePresident => "Phó chủ nhiệm",
        ClubRole.ClubAdmin => "Admin CLB",
        ClubRole.Member => "Thành viên",
        _ => role.ToString()
    };

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

    private async Task<bool> IsPresidentAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            m.RoleInClub == ClubRole.President);
}
