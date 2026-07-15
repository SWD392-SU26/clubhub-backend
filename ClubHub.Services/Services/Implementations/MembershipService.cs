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
    private readonly IAuditService _auditService;

    public MembershipService(IUnitOfWork uow, INotificationService notificationService, IAuditService auditService)
    {
        _uow = uow;
        _notificationService = notificationService;
        _auditService = auditService;
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

        await _auditService.LogAsync("ClubMember", membership.Id, "RequestJoin",
            userId, null, clubId, null, $"User {userId} requested to join club {clubId}");

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

        await _auditService.LogAsync("ClubMember", membership.Id, "CancelJoinRequest",
            userId, null, clubId, null, "User cancelled join request");

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

        await _auditService.LogAsync("ClubMember", membershipId, req.IsApproved ? "ApproveJoin" : "RejectJoin",
            reviewerId, null, membership.ClubId, null,
            req.IsApproved ? $"Duyệt đơn tham gia của user {membership.UserId}" : $"Từ chối đơn tham gia của user {membership.UserId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> LeaveClubAsync(Guid clubId, Guid userId)
    {
        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);
        if (membership == null || membership.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Bạn không phải thành viên CLB này.");

        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("Chủ nhiệm phải chuyển quyền hoặc đề xuất người kế nhiệm trước khi rời CLB.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubMember", membership.Id, "LeaveClub",
            userId, null, clubId, null, $"User {userId} left club {clubId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RemoveMemberAsync(Guid clubId, Guid memberId, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa thành viên.");

        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, memberId);
        if (membership == null || membership.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Thành viên không tồn tại.");
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

        await _auditService.LogAsync("ClubMember", membership.Id, "RemoveMember",
            requesterId, null, clubId, null, $"User {requesterId} removed member {memberId} from club {clubId}");

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
        if (membership == null || membership.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Thành viên không tồn tại.");

        if (req.NewRole != ClubRole.Member)
        {
            var targetUser = await _uow.Users.GetByIdAsync(req.UserId);
            if (targetUser == null || !targetUser.IsActive)
                return ApiResult<bool>.Failure(
                    "Không thể gán vai trò quản lý cho tài khoản đang bị khóa.");
        }

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

        var oldRole = membership.RoleInClub;
        membership.RoleInClub = req.NewRole;

        await _notificationService.SendNotificationAsync(
            req.UserId,
            "Vai trò trong CLB thay đổi",
            $"Vai trò của bạn trong CLB {club?.Name ?? ""} đã được cập nhật thành: {DisplayRole(req.NewRole)}.",
            "ROLE_CHANGED");

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubMember", membership.Id, "AssignRole",
            requesterId, null, clubId,
            $"{{\"oldRole\":\"{oldRole}\",\"newRole\":\"{req.NewRole}\"}}",
            $"User {requesterId} changed role of user {req.UserId} from {oldRole} to {req.NewRole}");

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
        var newAdminUser = await _uow.Users.GetByIdAsync(req.NewAdminUserId);
        if (newAdminUser == null || !newAdminUser.IsActive)
            return ApiResult<bool>.Failure("Không thể chuyển quyền cho tài khoản đang bị khóa.");

        currentAdmin.RoleInClub = ClubRole.Member;
        newAdmin.RoleInClub = ClubRole.President;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _notificationService.SendNotificationAsync(
            req.NewAdminUserId,
            "Chuyển quyền chủ nhiệm",
            $"Bạn đã được bổ nhiệm làm Chủ nhiệm CLB {club?.Name ?? ""}.",
            "TRANSFER_ADMIN");

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "TransferAdmin",
            currentAdminId, null, clubId, null,
            $"User {currentAdminId} transferred presidency to user {req.NewAdminUserId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> TransferAdminByUniversityAdminAsync(
        Guid clubId, TransferAdminRequest req, Guid universityAdminId)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null || club.Status is ClubStatus.Deleted or ClubStatus.Dissolved)
            return ApiResult<bool>.Failure("CLB không tồn tại hoặc đã giải tán.");

        var newAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.NewAdminUserId);
        if (newAdmin == null || newAdmin.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure(
                "Người nhận quyền không phải thành viên đang hoạt động của CLB.");

        var newAdminUser = await _uow.Users.GetByIdAsync(req.NewAdminUserId);
        if (newAdminUser == null || !newAdminUser.IsActive)
            return ApiResult<bool>.Failure("Không thể chuyển quyền cho tài khoản đang bị khóa.");

        var currentPresidents = await _uow.ClubMembers.Query()
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved &&
                        m.RoleInClub == ClubRole.President && m.UserId != req.NewAdminUserId)
            .ToListAsync();

        foreach (var president in currentPresidents)
            president.RoleInClub = ClubRole.Member;

        newAdmin.RoleInClub = ClubRole.President;
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            req.NewAdminUserId,
            "Chuyển quyền chủ nhiệm",
            $"University Admin đã bổ nhiệm bạn làm Chủ nhiệm CLB {club.Name}.",
            "TRANSFER_ADMIN");

        await _auditService.LogAsync(
            "ClubMember",
            newAdmin.Id,
            "TransferAdminByUniversityAdmin",
            universityAdminId,
            null,
            clubId,
            $"{{\"newAdminUserId\":\"{req.NewAdminUserId}\"}}",
            $"University Admin chuyển quyền chủ nhiệm CLB {club.Name} cho {newAdminUser.FullName}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> NominateSuccessorAsync(Guid clubId, Guid successorUserId, Guid currentAdminId)
    {
        var currentAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, currentAdminId);
        if (currentAdmin == null || currentAdmin.Status != MembershipStatus.Approved || currentAdmin.RoleInClub != ClubRole.President)
            return ApiResult<bool>.Failure("Bạn không phải chủ nhiệm CLB.");

        var successor = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, successorUserId);
        if (successor == null || successor.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Người kế nhiệm không phải thành viên CLB.");
        if (successor.UserId == currentAdminId)
            return ApiResult<bool>.Failure("Không thể đề cử chính mình.");

        // Store the successor nomination
        currentAdmin.SuccessorUserId = successorUserId;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            successorUserId,
            "Đề cử chủ nhiệm",
            $"Bạn đã được đề cử làm Chủ nhiệm CLB {club?.Name ?? ""}. Vui lòng vào mục thông báo để chấp nhận hoặc từ chối.",
            "SUCCESSION_NOMINATED");

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "NominateSuccessor",
            currentAdminId, null, clubId,
            $"{{\"successorUserId\":\"{successorUserId}\"}}",
            $"President {currentAdminId} nominated user {successorUserId} as successor");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AcceptSuccessionAsync(Guid clubId, Guid userId)
    {
        var currentPresident = await _uow.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.RoleInClub == ClubRole.President &&
            m.Status == MembershipStatus.Approved && m.SuccessorUserId == userId);

        if (currentPresident == null)
            return ApiResult<bool>.Failure("Bạn không được đề cử làm chủ nhiệm CLB này.");

        var successor = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);
        if (successor == null || successor.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Bạn không phải thành viên CLB.");

        // Transfer presidency
        currentPresident.RoleInClub = ClubRole.Member;
        currentPresident.SuccessorUserId = null;
        successor.RoleInClub = ClubRole.President;

        // Current president auto-leaves
        currentPresident.Status = MembershipStatus.Left;
        currentPresident.LeftAt = DateTime.UtcNow;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            currentPresident.UserId,
            "Chuyển giao chủ nhiệm thành công",
            $"Quyền chủ nhiệm CLB {club?.Name ?? ""} đã được chuyển giao thành công.",
            "SUCCESSION_COMPLETED");

        await _auditService.LogAsync("ClubMember", currentPresident.Id, "AcceptSuccession",
            userId, null, clubId, null,
            $"User {userId} accepted presidency succession from club {clubId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RejectSuccessionAsync(Guid clubId, Guid userId)
    {
        var currentPresident = await _uow.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.RoleInClub == ClubRole.President &&
            m.Status == MembershipStatus.Approved && m.SuccessorUserId == userId);

        if (currentPresident == null)
            return ApiResult<bool>.Failure("Bạn không được đề cử làm chủ nhiệm CLB này.");

        currentPresident.SuccessorUserId = null;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            currentPresident.UserId,
            "Từ chối kế nhiệm",
            $"Người được đề cử đã từ chối làm Chủ nhiệm CLB {club?.Name ?? ""}. Vui lòng chọn người khác.",
            "SUCCESSION_REJECTED");

        await _auditService.LogAsync("ClubMember", currentPresident.Id, "RejectSuccession",
            userId, null, clubId, null,
            $"User {userId} rejected presidency succession from club {clubId}");

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

    public async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

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

    private async Task<bool> IsPresidentAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            m.RoleInClub == ClubRole.President);
}
