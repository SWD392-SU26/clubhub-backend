using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IClubRepository : IGenericRepository<Club>
{
    Task<Club?> GetClubWithMembersAsync(Guid clubId);
    IQueryable<Club> QueryActiveClubs();
    IQueryable<Club> QueryMyClubs(Guid userId);
}
