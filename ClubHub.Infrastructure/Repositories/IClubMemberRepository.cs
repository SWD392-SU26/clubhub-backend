using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IClubMemberRepository : IGenericRepository<ClubMember>
{
    Task<bool> IsClubAdminAsync(Guid clubId, Guid userId);
    Task<bool> IsMemberAsync(Guid clubId, Guid userId);
    Task<ClubMember?> GetByUserAndClubAsync(Guid clubId, Guid userId);
    IQueryable<ClubMember> QueryPendingRequests(Guid clubId);
    IQueryable<ClubMember> QueryApprovedMembers(Guid clubId);
    IQueryable<ClubMember> QueryMyMemberships(Guid userId);
}
