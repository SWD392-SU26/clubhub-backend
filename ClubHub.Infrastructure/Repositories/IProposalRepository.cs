using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IProposalRepository : IGenericRepository<ClubProposal>
{
    Task<ClubProposal?> GetWithSubmitterAsync(Guid proposalId);
    IQueryable<ClubProposal> QueryAll();
    IQueryable<ClubProposal> QueryMyProposals(Guid userId);
}
