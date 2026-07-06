using ClubHub.API.Data;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class ClubMemberRepository : GenericRepository<ClubMember>, IClubMemberRepository
{
    public ClubMemberRepository(AppDbContext context) : base(context) { }

    public async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _dbSet.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

    public async Task<bool> IsMemberAsync(Guid clubId, Guid userId)
        => await _dbSet.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

    public async Task<ClubMember?> GetByUserAndClubAsync(Guid clubId, Guid userId)
        => await _dbSet.FirstOrDefaultAsync(m =>
            m.ClubId == clubId && m.UserId == userId);

    public IQueryable<ClubMember> QueryPendingRequests(Guid clubId)
        => _dbSet
            .Include(m => m.User)
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending)
            .OrderBy(m => m.RequestedAt);

    public IQueryable<ClubMember> QueryApprovedMembers(Guid clubId)
        => _dbSet
            .Include(m => m.User)
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved)
            .OrderBy(m => m.JoinedAt);

    public IQueryable<ClubMember> QueryMyMemberships(Guid userId)
        => _dbSet
            .Include(m => m.Club)
            .Where(m => m.UserId == userId);
}
