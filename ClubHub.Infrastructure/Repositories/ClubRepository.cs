using ClubHub.API.Data;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class ClubRepository : GenericRepository<Club>, IClubRepository
{
    public ClubRepository(AppDbContext context) : base(context) { }

    public async Task<Club?> GetClubWithMembersAsync(Guid clubId)
        => await _dbSet
            .Include(c => c.Members.Where(m => m.Status == MembershipStatus.Approved))
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == clubId && c.Status != ClubStatus.Deleted);

    public IQueryable<Club> QueryActiveClubs()
        => _dbSet.Where(c => c.Status == ClubStatus.Active);

    public IQueryable<Club> QueryMyClubs(Guid userId)
        => _dbSet.Where(c => c.Members.Any(m =>
            m.UserId == userId && m.Status == MembershipStatus.Approved)
            && c.Status != ClubStatus.Deleted);
}
