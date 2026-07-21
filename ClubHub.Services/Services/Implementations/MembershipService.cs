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

        if (existing != null && existing.Status != MembershipStatus.Pending)
        {
            throw new InvalidOperationException("Unexpected membership status for existing membership.");
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

            // When a member is approved, update their User.Role to ClubMember if still Student
            var user = await _uow.Users.GetByIdAsync(membership.UserId);
            if (user != null && user.Role == Role.Student)
            {
                user.Role = Role.ClubMember;
                user.UpdatedAt = DateTime.UtcNow;
            }

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

        if (membership.RoleInClub == Role.ClubAdmin)
            return ApiResult<bool>.Failure("Admin CLB phải chuyển quyền trước khi rời CLB.");

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
        if (membership.RoleInClub == Role.ClubAdmin)
            return ApiResult<bool>.Failure("Không thể xóa Admin CLB.");

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

        var membership = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.UserId);
        if (membership == null || membership.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Thành viên không tồn tại.");

        var club = await _uow.Clubs.GetByIdAsync(clubId);
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
        if (currentAdmin == null || currentAdmin.Status != MembershipStatus.Approved || currentAdmin.RoleInClub != Role.ClubAdmin)
            return ApiResult<bool>.Failure("Bạn không phải Admin CLB.");

        var newAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, req.NewAdminUserId);
        if (newAdmin == null || newAdmin.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Người nhận quyền không phải thành viên CLB.");

        // Update both User.Role fields
        var oldAdminUser = await _uow.Users.GetByIdAsync(currentAdminId);
        if (oldAdminUser != null)
        {
            oldAdminUser.Role = Role.ClubMember;
            oldAdminUser.UpdatedAt = DateTime.UtcNow;
        }

        var newAdminUser = await _uow.Users.GetByIdAsync(req.NewAdminUserId);
        if (newAdminUser != null)
        {
            newAdminUser.Role = Role.ClubAdmin;
            newAdminUser.UpdatedAt = DateTime.UtcNow;
        }

        currentAdmin.RoleInClub = Role.ClubMember;
        newAdmin.RoleInClub = Role.ClubAdmin;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _notificationService.SendNotificationAsync(
            req.NewAdminUserId,
            "Chuyển quyền Admin CLB",
            $"Bạn đã được bổ nhiệm làm Admin CLB {club?.Name ?? ""}.",
            "TRANSFER_ADMIN");

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "TransferAdmin",
            currentAdminId, null, clubId, null,
            $"User {currentAdminId} transferred admin to user {req.NewAdminUserId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> NominateSuccessorAsync(Guid clubId, Guid successorUserId, Guid currentAdminId)
    {
        var currentAdmin = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, currentAdminId);
        if (currentAdmin == null || currentAdmin.Status != MembershipStatus.Approved || currentAdmin.RoleInClub != Role.ClubAdmin)
            return ApiResult<bool>.Failure("Bạn không phải Admin CLB.");

        var successor = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, successorUserId);
        if (successor == null || successor.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Người kế nhiệm không phải thành viên CLB.");
        if (successor.UserId == currentAdminId)
            return ApiResult<bool>.Failure("Không thể đề cử chính mình.");

        currentAdmin.SuccessorUserId = successorUserId;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            successorUserId,
            "Đề cử Admin CLB",
            $"Bạn đã được đề cử làm Admin CLB {club?.Name ?? ""}. Vui lòng vào mục thông báo để chấp nhận hoặc từ chối.",
            "SUCCESSION_NOMINATED");

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "NominateSuccessor",
            currentAdminId, null, clubId,
            $"{{\"successorUserId\":\"{successorUserId}\"}}",
            $"Admin {currentAdminId} nominated user {successorUserId} as successor");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AcceptSuccessionAsync(Guid clubId, Guid userId)
    {
        var currentAdmin = await _uow.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.RoleInClub == Role.ClubAdmin &&
            m.Status == MembershipStatus.Approved && m.SuccessorUserId == userId);

        if (currentAdmin == null)
            return ApiResult<bool>.Failure("Bạn không được đề cử làm Admin CLB này.");

        var successor = await _uow.ClubMembers.GetByUserAndClubAsync(clubId, userId);
        if (successor == null || successor.Status != MembershipStatus.Approved)
            return ApiResult<bool>.Failure("Bạn không phải thành viên CLB.");

        // Update User.Role fields
        var oldAdminUser = await _uow.Users.GetByIdAsync(currentAdmin.UserId);
        if (oldAdminUser != null) { oldAdminUser.Role = Role.ClubMember; oldAdminUser.UpdatedAt = DateTime.UtcNow; }

        var newAdminUser = await _uow.Users.GetByIdAsync(userId);
        if (newAdminUser != null) { newAdminUser.Role = Role.ClubAdmin; newAdminUser.UpdatedAt = DateTime.UtcNow; }

        // Transfer admin in club
        currentAdmin.RoleInClub = Role.ClubMember;
        currentAdmin.SuccessorUserId = null;
        successor.RoleInClub = Role.ClubAdmin;

        // Current admin auto-leaves
        currentAdmin.Status = MembershipStatus.Left;
        currentAdmin.LeftAt = DateTime.UtcNow;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            currentAdmin.UserId,
            "Chuyển giao Admin thành công",
            $"Quyền Admin CLB {club?.Name ?? ""} đã được chuyển giao thành công.",
            "SUCCESSION_COMPLETED");

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "AcceptSuccession",
            userId, null, clubId, null,
            $"User {userId} accepted admin succession from club {clubId}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RejectSuccessionAsync(Guid clubId, Guid userId)
    {
        var currentAdmin = await _uow.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.RoleInClub == Role.ClubAdmin &&
            m.Status == MembershipStatus.Approved && m.SuccessorUserId == userId);

        if (currentAdmin == null)
            return ApiResult<bool>.Failure("Bạn không được đề cử làm Admin CLB này.");

        currentAdmin.SuccessorUserId = null;

        var club = await _uow.Clubs.GetByIdAsync(clubId);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            currentAdmin.UserId,
            "Từ chối kế nhiệm",
            $"Người được đề cử đã từ chối làm Admin CLB {club?.Name ?? ""}. Vui lòng chọn người khác.",
            "SUCCESSION_REJECTED");

        await _auditService.LogAsync("ClubMember", currentAdmin.Id, "RejectSuccession",
            userId, null, clubId, null,
            $"User {userId} rejected admin succession from club {clubId}");

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
            m.RoleInClub == Role.ClubAdmin);

    public async Task<List<MyMembershipDto>> GetMyMembershipsAsync(Guid userId)
    {
        return await _uow.ClubMembers.QueryMyMemberships(userId)
            .Select(m => new MyMembershipDto(
                m.ClubId, m.Club.Name, m.Club.LogoUrl,
                m.RoleInClub.ToString(), m.Status.ToString(),
                m.RequestedAt, m.JoinedAt))
            .ToListAsync();
    }

    private static string DisplayRole(Role role) => role switch
    {
        Role.ClubAdmin => "Admin CLB",
        Role.ClubMember => "Thành viên",
        _ => role.ToString()
    };
}
