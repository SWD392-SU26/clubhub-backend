using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public MembershipService(AppDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    public async Task<ApiResult<bool>> RequestJoinAsync(Guid clubId, Guid userId, JoinClubRequest req)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null || club.Status != ClubStatus.Active)
            return ApiResult<bool>.Failure("Club does not exist or is not active.");

        var existing = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == userId);

        if (existing != null)
        {
            if (existing.Status == MembershipStatus.Pending)
                return ApiResult<bool>.Failure("You already submitted a join request for this club.");
            if (existing.Status == MembershipStatus.Approved)
                return ApiResult<bool>.Failure("You are already a member of this club.");
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
        await _auditLogService.LogAsync(clubId, userId, "JoinRequestCreated", nameof(ClubMember), membership.Id, userId, req.JoinReason);

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> ReviewRequestAsync(Guid membershipId, Guid reviewerId, ReviewMembershipRequest req)
    {
        var membership = await _db.ClubMembers.FindAsync(membershipId);
        if (membership == null) return ApiResult<bool>.Failure("Join request does not exist.");
        if (membership.Status != MembershipStatus.Pending)
            return ApiResult<bool>.Failure("This join request has already been reviewed.");

        if (!await IsClubAdminAsync(membership.ClubId, reviewerId))
            return ApiResult<bool>.Failure("You do not have permission to review this join request.");
        if (membership.UserId == reviewerId)
            return ApiResult<bool>.Failure("You cannot approve or reject your own join request.");

        membership.Status = req.IsApproved ? MembershipStatus.Approved : MembershipStatus.Rejected;
        membership.ReviewedBy = reviewerId;
        membership.ReviewedAt = DateTime.UtcNow;

        if (req.IsApproved)
            membership.JoinedAt = DateTime.UtcNow;
        else
            membership.RejectionReason = req.RejectionReason;

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            membership.ClubId,
            reviewerId,
            req.IsApproved ? "JoinRequestApproved" : "JoinRequestRejected",
            nameof(ClubMember),
            membership.Id,
            membership.UserId,
            req.IsApproved ? null : req.RejectionReason);

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> LeaveClubAsync(Guid clubId, Guid userId)
    {
        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("You are not a member of this club.");
        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("The president must transfer the role before leaving the club.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, userId, "MemberLeft", nameof(ClubMember), membership.Id, userId);

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RemoveMemberAsync(Guid clubId, Guid memberId, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("You do not have permission to remove members.");

        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == memberId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("Member does not exist.");
        if (membership.RoleInClub == ClubRole.President)
            return ApiResult<bool>.Failure("The club president cannot be removed.");

        membership.Status = MembershipStatus.Left;
        membership.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, "MemberRemoved", nameof(ClubMember), membership.Id, memberId);

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> AssignRoleAsync(Guid clubId, AssignRoleRequest req, Guid requesterId)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("You do not have permission to assign member roles.");

        var membership = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == req.UserId && m.Status == MembershipStatus.Approved);

        if (membership == null) return ApiResult<bool>.Failure("Member does not exist.");

        membership.RoleInClub = req.NewRole;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, "MemberRoleAssigned", nameof(ClubMember), membership.Id, req.UserId, $"Assigned role {req.NewRole}.");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> TransferAdminAsync(Guid clubId, TransferAdminRequest req, Guid currentAdminId)
    {
        var isUniversityAdmin = await _db.Users.AnyAsync(u =>
            u.Id == currentAdminId && u.SystemRole == SystemRole.UniversityAdmin);

        var currentAdmin = isUniversityAdmin
            ? await _db.ClubMembers.FirstOrDefaultAsync(m =>
                m.ClubId == clubId &&
                m.Status == MembershipStatus.Approved &&
                m.RoleInClub == ClubRole.President)
            : await _db.ClubMembers.FirstOrDefaultAsync(m =>
                m.ClubId == clubId && m.UserId == currentAdminId &&
                m.Status == MembershipStatus.Approved && m.RoleInClub == ClubRole.President);

        if (!isUniversityAdmin && currentAdmin == null)
            return ApiResult<bool>.Failure("Only the club president can transfer the president role.");

        var newAdmin = await _db.ClubMembers.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == req.NewAdminUserId && m.Status == MembershipStatus.Approved);

        if (newAdmin == null)
            return ApiResult<bool>.Failure("The target user is not an approved club member.");

        if (currentAdmin != null)
            currentAdmin.RoleInClub = ClubRole.Member;

        newAdmin.RoleInClub = ClubRole.President;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, currentAdminId, "PresidentTransferred", nameof(ClubMember), newAdmin.Id, req.NewAdminUserId);

        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<MembershipRequestDto>> GetPendingRequestsAsync(Guid clubId, Guid requesterId, int page, int pageSize)
    {
        if (!await IsClubAdminAsync(clubId, requesterId))
            throw new UnauthorizedAccessException("You do not have permission to view pending join requests.");

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

    public async Task<PagedResult<ClubMemberDto>> GetMembersAsync(Guid clubId, Guid requesterId, int page, int pageSize)
    {
        if (!await CanReadMembersAsync(clubId, requesterId))
            throw new UnauthorizedAccessException("You must be a club member to view the member list.");

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
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private async Task<bool> CanReadMembersAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);
}
