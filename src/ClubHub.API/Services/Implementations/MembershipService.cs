using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _db;

    public MembershipService(AppDbContext db) => _db = db;

    public async Task<ApiResult<bool>> RequestJoinAsync(Guid clubId, Guid userId, JoinClubRequest req)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null || club.Status != ClubStatus.Active)
            return ApiResult<bool>.Failure("CLB không tồn tại hoặc không hoạt động.");

        var existing = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == userId);

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

        _db.ClubMembers.Add(membership);
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> ReviewRequestAsync(Guid membershipId, Guid reviewerId, ReviewMembershipRequest req)
    {
        var membership = await _db.ClubMembers.FindAsync(membershipId);
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

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> LeaveClubAsync(Guid clubId, Guid userId)
    {
        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("Bạn không phải thành viên CLB này.");

        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("Chủ nhiệm phải chuyển quyền trước khi rời CLB.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RemoveMemberAsync(Guid clubId, Guid memberId, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa thành viên.");

        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == memberId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("Thành viên không tồn tại.");
        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("Không thể xóa chủ nhiệm CLB.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AssignRoleAsync(Guid clubId, AssignRoleRequest req, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền gán vai trò.");

        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == req.UserId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("Thành viên không tồn tại.");

        membership.RoleInClub = req.NewRole;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> TransferAdminAsync(Guid clubId, TransferAdminRequest req, Guid currentAdminId)
    {
        var currentAdmin = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == currentAdminId &&
            m.Status == MembershipStatus.Approved && m.RoleInClub == ClubRole.President);

        if (currentAdmin == null)
            return ApiResult<bool>.Failure("Bạn không phải chủ nhiệm CLB.");

        var newAdmin = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == req.NewAdminUserId && m.Status == MembershipStatus.Approved);

        if (newAdmin == null)
            return ApiResult<bool>.Failure("Người nhận quyền không phải thành viên CLB.");

        currentAdmin.RoleInClub = ClubRole.Member;
        newAdmin.RoleInClub = ClubRole.President;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<MembershipRequestDto>> GetPendingRequestsAsync(Guid clubId, int page, int pageSize)
    {
        var query = _db.ClubMembers
            .Include(m => m.User)
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending)
            .OrderBy(m => m.RequestedAt);

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
        var query = _db.ClubMembers
            .Include(m => m.User)
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved)
            .OrderBy(m => m.JoinedAt);

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
        return await _db.ClubMembers
            .Include(m => m.Club)
            .Where(m => m.UserId == userId)
            .Select(m => new MyMembershipDto(
                m.ClubId, m.Club.Name, m.Club.LogoUrl,
                m.RoleInClub.ToString(), m.Status.ToString(),
                m.RequestedAt, m.JoinedAt))
            .ToListAsync();
    }

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));
}
