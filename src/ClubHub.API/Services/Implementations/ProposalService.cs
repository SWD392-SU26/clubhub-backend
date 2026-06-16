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
            FounderInfo = req.FounderInfo,
            FounderStudentCode = req.FounderStudentCode,
            FounderIdCardUrl = req.FounderIdCardUrl,
            ContactEmail = req.ContactEmail,
            ContactPhone = req.ContactPhone,
            Advisor = req.Advisor,
            LogoUrl = req.LogoUrl,
            ProposalFileUrl = req.ProposalFileUrl,
            Notes = req.Notes,
            SubmittedBy = submittedBy
        };

        _db.ClubProposals.Add(proposal);
        await _db.SaveChangesAsync();
        return ApiResult<ProposalDto>.Success(MapToDto(proposal));
    }

    public async Task<ApiResult<bool>> ReviewAsync(Guid proposalId, ReviewProposalRequest req, Guid reviewerId)
    {
        var proposal = await _db.ClubProposals.FindAsync(proposalId);
        if (proposal == null) return ApiResult<bool>.Failure("Hồ sơ không tồn tại.");
        if (proposal.Status != ProposalStatus.Pending && proposal.Status != ProposalStatus.NeedsRevision)
            return ApiResult<bool>.Failure("Hồ sơ này đã được xử lý.");

        proposal.ReviewedBy = reviewerId;
        proposal.ReviewedAt = DateTime.UtcNow;

        if (req.IsApproved)
        {
            proposal.Status = ProposalStatus.Approved;

            // Auto-create the club
            await _clubService.CreateClubAsync(new CreateClubRequest(
                proposal.ClubName, proposal.Category,
                proposal.Description, proposal.LogoUrl, null),
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
        if (proposal == null) return ApiResult<bool>.Failure("Hồ sơ không tồn tại.");
        if (proposal.Status != ProposalStatus.Pending)
            return ApiResult<bool>.Failure("Chỉ có thể yêu cầu bổ sung khi hồ sơ đang chờ xử lý.");

        proposal.Status = ProposalStatus.NeedsRevision;
        proposal.RejectionReason = req.RevisionNote;
        proposal.ReviewedBy = reviewerId;
        proposal.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ProposalDetailDto?> GetByIdAsync(Guid proposalId)
    {
        var p = await _db.ClubProposals
            .Include(pr => pr.Submitter)
            .FirstOrDefaultAsync(pr => pr.Id == proposalId);

        if (p == null) return null;

        return new ProposalDetailDto(
            p.Id, p.ClubName, p.Category.ToString(), p.Description,
            p.Mission, p.Reason, p.ActivityPlan, p.FounderInfo,
            p.FounderStudentCode, p.FounderIdCardUrl, p.ContactEmail,
            p.ContactPhone, p.Advisor, p.LogoUrl, p.ProposalFileUrl,
            p.Notes, p.RejectionReason, p.Status.ToString(),
            p.Submitter.FullName, p.SubmittedAt, p.ReviewedAt);
    }

    public async Task<PagedResult<ProposalDto>> GetAllAsync(string? status, int page, int pageSize)
    {
        var query = _db.ClubProposals.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProposalStatus>(status, true, out var s))
            query = query.Where(p => p.Status == s);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
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
        p.Id, p.ClubName, p.Category.ToString(), p.Description,
        p.Mission, p.Status.ToString(), p.FounderInfo,
        p.FounderStudentCode, p.ContactEmail, p.RejectionReason,
        p.SubmittedAt, p.ReviewedAt);
}
