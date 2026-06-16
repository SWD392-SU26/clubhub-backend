using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IProposalService
{
    Task<ApiResult<ProposalDto>> SubmitAsync(SubmitProposalRequest request, Guid submittedBy);
    Task<ApiResult<bool>> ReviewAsync(Guid proposalId, ReviewProposalRequest request, Guid reviewerId);
    Task<ApiResult<bool>> RequestRevisionAsync(Guid proposalId, RequestRevisionRequest request, Guid reviewerId);
    Task<ProposalDetailDto?> GetByIdAsync(Guid proposalId);
    Task<PagedResult<ProposalDto>> GetAllAsync(string? status, int page, int pageSize);
    Task<List<ProposalDto>> GetMyProposalsAsync(Guid userId);
}
