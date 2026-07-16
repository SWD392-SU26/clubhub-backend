using ClubHub.API.Data;
using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class ProposalService : IProposalService
{
    private readonly AppDbContext _db;
    private readonly IClubService _clubService;

    public ProposalService(AppDbContext db, IClubService clubService)
    {
        _db = db;
        _clubService = clubService;
    }

    public async Task<ApiResult<ProposalDto>> SubmitAsync(SubmitProposalRequest req, Guid submittedBy)
    {
        var proposal = new ClubProposal
        {
            ClubName = req.ClubName,
            Category = req.Category,
            Description = req.Description,
            Mission = req.Mission,
            Reason = req.Reason,
            ActivityPlan = req.ActivityPlan,
            FounderInfo = req.FounderFullName,
            FounderStudentCode = req.FounderStudentCode,
            FounderIdCardUrl = req.FounderIdentityDocumentUrl,
            ContactEmail = req.ContactEmail,
            ContactPhone = req.ContactPhone,
            Advisor = req.AdvisorName,
            LogoUrl = req.LogoUrl,
            ProposalFileUrl = req.ProposalFileUrl,
            Notes = req.AdditionalNote,
            SubmittedBy = submittedBy
        };

        _db.ClubProposals.Add(proposal);
        await _db.SaveChangesAsync();
        return ApiResult<ProposalDto>.Success(MapToDto(proposal));
    }

    public async Task<ApiResult<bool>> ReviewAsync(Guid proposalId, ReviewProposalRequest req, Guid reviewerId)
    {
        var proposal = await _db.ClubProposals.FindAsync(proposalId);
        if (proposal == null) return ApiResult<bool>.Failure("Proposal does not exist.");
        if (proposal.Status != ProposalStatus.Pending && proposal.Status != ProposalStatus.NeedMoreInfo)
            return ApiResult<bool>.Failure("This proposal has already been reviewed.");

        proposal.ReviewedBy = reviewerId;
        proposal.ReviewedAt = DateTime.UtcNow;

        if (req.IsApproved)
        {
            proposal.Status = ProposalStatus.Approved;
            proposal.RejectionReason = null;

            await _clubService.CreateClubAsync(new CreateClubRequest(
                    proposal.ClubName,
                    proposal.Category,
                    proposal.Description,
                    proposal.LogoUrl,
                    null),
                proposal.SubmittedBy);
        }
        else
        {
            proposal.Status = ProposalStatus.Rejected;
            proposal.RejectionReason = req.RejectionReason;
        }

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RequestRevisionAsync(Guid proposalId, RequestRevisionRequest req, Guid reviewerId)
    {
        var proposal = await _db.ClubProposals.FindAsync(proposalId);
        if (proposal == null) return ApiResult<bool>.Failure("Proposal does not exist.");
        if (proposal.Status != ProposalStatus.Pending)
            return ApiResult<bool>.Failure("Only pending proposals can be sent back for more information.");

        proposal.Status = ProposalStatus.NeedMoreInfo;
        proposal.RejectionReason = req.RevisionNote;
        proposal.RevisionNote = req.RevisionNote;
        proposal.RequestedRevisionBy = reviewerId;
        proposal.RequestedRevisionAt = DateTime.UtcNow;
        proposal.ReviewedBy = reviewerId;
        proposal.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<ProposalDetailDto>> UpdateAsync(Guid proposalId, UpdateProposalRequest req, Guid userId)
    {
        var proposal = await _db.ClubProposals
            .Include(p => p.Submitter)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal == null) return ApiResult<ProposalDetailDto>.Failure("Proposal does not exist.");
        if (proposal.SubmittedBy != userId) return ApiResult<ProposalDetailDto>.Failure("You can only edit your own proposal.");
        if (proposal.Status is ProposalStatus.Approved or ProposalStatus.Rejected)
            return ApiResult<ProposalDetailDto>.Failure("Approved or rejected proposals cannot be edited.");

        ApplyUpdate(proposal, req);
        await _db.SaveChangesAsync();

        return ApiResult<ProposalDetailDto>.Success(MapToDetailDto(proposal));
    }

    public async Task<ApiResult<ProposalDetailDto>> ResubmitAsync(Guid proposalId, Guid userId)
    {
        var proposal = await _db.ClubProposals
            .Include(p => p.Submitter)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal == null) return ApiResult<ProposalDetailDto>.Failure("Proposal does not exist.");
        if (proposal.SubmittedBy != userId) return ApiResult<ProposalDetailDto>.Failure("You can only resubmit your own proposal.");
        if (proposal.Status != ProposalStatus.NeedMoreInfo)
            return ApiResult<ProposalDetailDto>.Failure("Only proposals that need more information can be resubmitted.");

        proposal.Status = ProposalStatus.Pending;
        proposal.RejectionReason = null;
        proposal.ReviewedBy = null;
        proposal.ReviewedAt = null;
        proposal.ResubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResult<ProposalDetailDto>.Success(MapToDetailDto(proposal));
    }

    public async Task<ProposalDetailDto?> GetByIdAsync(Guid proposalId)
    {
        var proposal = await _db.ClubProposals
            .Include(p => p.Submitter)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        return proposal == null ? null : MapToDetailDto(proposal);
    }

    public async Task<PagedResult<ProposalDto>> GetAllAsync(string? status, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ClubProposals.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProposalStatus>(status, true, out var parsed))
            query = query.Where(p => p.Status == parsed);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PagedResult<ProposalDto>(items, page, pageSize, total);
    }

    public async Task<List<ProposalDto>> GetMyProposalsAsync(Guid userId)
    {
        return await _db.ClubProposals
            .Where(p => p.SubmittedBy == userId)
            .OrderByDescending(p => p.SubmittedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    private static ProposalDto MapToDto(ClubProposal p) => new(
        p.Id,
        p.ClubName,
        p.Category.ToString(),
        p.Description,
        p.Mission,
        p.Status.ToString(),
        p.FounderInfo,
        p.FounderStudentCode,
        p.ContactEmail,
        p.RejectionReason,
        p.SubmittedAt,
        p.ReviewedAt);

    private static ProposalDetailDto MapToDetailDto(ClubProposal p) => new(
        p.Id,
        p.ClubName,
        p.Category.ToString(),
        p.Description,
        p.Mission,
        p.Reason,
        p.ActivityPlan,
        p.FounderInfo,
        p.FounderStudentCode,
        p.FounderIdCardUrl,
        p.ContactEmail,
        p.ContactPhone,
        p.Advisor,
        p.LogoUrl,
        p.ProposalFileUrl,
        p.Notes,
        p.RejectionReason,
        p.RevisionNote,
        p.RequestedRevisionAt,
        p.ResubmittedAt,
        p.Status.ToString(),
        p.Submitter.FullName,
        p.SubmittedAt,
        p.ReviewedAt);

    private static void ApplyUpdate(ClubProposal proposal, UpdateProposalRequest req)
    {
        if (req.ClubName != null) proposal.ClubName = req.ClubName;
        if (req.Category.HasValue) proposal.Category = req.Category.Value;
        if (req.Description != null) proposal.Description = req.Description;
        if (req.Mission != null) proposal.Mission = req.Mission;
        if (req.Reason != null) proposal.Reason = req.Reason;
        if (req.ActivityPlan != null) proposal.ActivityPlan = req.ActivityPlan;
        if (req.FounderFullName != null) proposal.FounderInfo = req.FounderFullName;
        if (req.FounderStudentCode != null) proposal.FounderStudentCode = req.FounderStudentCode;
        if (req.FounderIdentityDocumentUrl != null) proposal.FounderIdCardUrl = req.FounderIdentityDocumentUrl;
        if (req.ContactEmail != null) proposal.ContactEmail = req.ContactEmail;
        if (req.ContactPhone != null) proposal.ContactPhone = req.ContactPhone;
        if (req.AdvisorName != null) proposal.Advisor = req.AdvisorName;
        if (req.LogoUrl != null) proposal.LogoUrl = req.LogoUrl;
        if (req.ProposalFileUrl != null) proposal.ProposalFileUrl = req.ProposalFileUrl;
        if (req.AdditionalNote != null) proposal.Notes = req.AdditionalNote;
    }
}
