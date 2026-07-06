using ClubHub.API.Data;
using ClubHub.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class ProposalRepository : GenericRepository<ClubProposal>, IProposalRepository
{
    public ProposalRepository(AppDbContext context) : base(context) { }

    public async Task<ClubProposal?> GetWithSubmitterAsync(Guid proposalId)
        => await _dbSet
            .Include(pr => pr.Submitter)
            .FirstOrDefaultAsync(pr => pr.Id == proposalId);

    public IQueryable<ClubProposal> QueryAll()
        => _dbSet.OrderByDescending(p => p.SubmittedAt);

    public IQueryable<ClubProposal> QueryMyProposals(Guid userId)
        => _dbSet
            .Where(p => p.SubmittedBy == userId)
            .OrderByDescending(p => p.SubmittedAt);
}
