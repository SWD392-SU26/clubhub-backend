using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class ProposalService : IProposalService
{
    private readonly IUnitOfWork _uow;
    private readonly IClubService _clubService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    public ProposalService(IUnitOfWork uow, IClubService clubService,
        INotificationService notificationService, IAuditService auditService)
    {
        _uow = uow;
        _clubService = clubService;
        _notificationService = notificationService;
        _auditService = auditService;
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

        _uow.Proposals.Add(proposal);
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Proposal", proposal.Id, "Submit",
            submittedBy, null, null, null, $"Nộp hồ sơ thành lập CLB: {proposal.ClubName}");

        return ApiResult<ProposalDto>.Success(MapToDto(proposal));
    }

    public async Task<ApiResult<ProposalDto>> ResubmitAsync(Guid proposalId, SubmitProposalRequest req, Guid submittedBy)
    {
        var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
        if (proposal == null) return ApiResult<ProposalDto>.Failure("Hồ sơ không tồn tại.");
        if (proposal.Status != ProposalStatus.NeedsRevision)
            return ApiResult<ProposalDto>.Failure("Chỉ có thể nộp lại hồ sơ khi được yêu cầu bổ sung.");
        if (proposal.SubmittedBy != submittedBy)
            return ApiResult<ProposalDto>.Failure("Bạn không phải người gửi hồ sơ này.");

        // Save current state as a revision before updating
        var revisionNumber = await _uow.ProposalRevisions.CountAsync(r => r.ProposalId == proposalId) + 1;

        var revision = new ProposalRevision
        {
            ProposalId = proposalId,
            RevisionNumber = revisionNumber,
            ClubName = proposal.ClubName,
            Category = proposal.Category.ToString(),
            Description = proposal.Description,
            Mission = proposal.Mission,
            Reason = proposal.Reason,
            ActivityPlan = proposal.ActivityPlan,
            FounderInfo = proposal.FounderInfo,
            FounderStudentCode = proposal.FounderStudentCode,
            FounderIdCardUrl = proposal.FounderIdCardUrl,
            ContactEmail = proposal.ContactEmail,
            ContactPhone = proposal.ContactPhone,
            Advisor = proposal.Advisor,
            LogoUrl = proposal.LogoUrl,
            ProposalFileUrl = proposal.ProposalFileUrl,
            Notes = proposal.Notes,
            RevisionNote = proposal.RejectionReason
        };
        _uow.ProposalRevisions.Add(revision);

        // Update proposal fields
        proposal.ClubName = req.ClubName;
        proposal.Category = req.Category;
        proposal.Description = req.Description;
        proposal.Mission = req.Mission;
        proposal.Reason = req.Reason;
        proposal.ActivityPlan = req.ActivityPlan;
        proposal.FounderInfo = req.FounderInfo;
        proposal.FounderStudentCode = req.FounderStudentCode;
        proposal.FounderIdCardUrl = req.FounderIdCardUrl;
        proposal.ContactEmail = req.ContactEmail;
        proposal.ContactPhone = req.ContactPhone;
        proposal.Advisor = req.Advisor;
        proposal.LogoUrl = req.LogoUrl;
        proposal.ProposalFileUrl = req.ProposalFileUrl;
        proposal.Notes = req.Notes;
        proposal.Status = ProposalStatus.Pending;
        proposal.RejectionReason = null;
        proposal.ReviewedBy = null;
        proposal.ReviewedAt = null;

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Proposal", proposalId, "Resubmit",
            submittedBy, null, null,
            $"{{\"revisionNumber\":{revisionNumber}}}",
            $"Nộp lại hồ sơ (lần {revisionNumber}): {proposal.ClubName}");

        return ApiResult<ProposalDto>.Success(MapToDto(proposal));
    }

    public async Task<ApiResult<bool>> ReviewAsync(Guid proposalId, ReviewProposalRequest req, Guid reviewerId)
    {
        var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
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

            await _notificationService.SendNotificationAsync(
                proposal.SubmittedBy,
                "Hồ sơ thành lập CLB được duyệt",
                $"Chúc mừng! Hồ sơ thành lập CLB {proposal.ClubName} của bạn đã được duyệt. CLB đã được tạo tự động.",
                "PROPOSAL_APPROVED");

            await _auditService.LogAsync("Proposal", proposalId, "Approve",
                reviewerId, null, null, null, $"Duyệt hồ sơ thành lập CLB: {proposal.ClubName}");
        }
        else
        {
            proposal.Status = ProposalStatus.Rejected;
            proposal.RejectionReason = req.RejectionReason;

            await _notificationService.SendNotificationAsync(
                proposal.SubmittedBy,
                "Hồ sơ thành lập CLB bị từ chối",
                $"Hồ sơ thành lập CLB {proposal.ClubName} của bạn đã bị từ chối. " +
                $"Lý do: {req.RejectionReason ?? "Không rõ"}",
                "PROPOSAL_REJECTED");

            await _auditService.LogAsync("Proposal", proposalId, "Reject",
                reviewerId, null, null, null, $"Từ chối hồ sơ thành lập CLB: {proposal.ClubName}");
        }

        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RequestRevisionAsync(Guid proposalId, RequestRevisionRequest req, Guid reviewerId)
    {
        var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
        if (proposal == null) return ApiResult<bool>.Failure("Hồ sơ không tồn tại.");
        if (proposal.Status != ProposalStatus.Pending)
            return ApiResult<bool>.Failure("Chỉ có thể yêu cầu bổ sung khi hồ sơ đang chờ xử lý.");

        proposal.Status = ProposalStatus.NeedsRevision;
        proposal.RejectionReason = req.RevisionNote;
        proposal.ReviewedBy = reviewerId;
        proposal.ReviewedAt = DateTime.UtcNow;

        await _notificationService.SendNotificationAsync(
            proposal.SubmittedBy,
            "Hồ sơ thành lập CLB yêu cầu bổ sung",
            $"Hồ sơ thành lập CLB {proposal.ClubName} của bạn cần được bổ sung. " +
            $"Ghi chú: {req.RevisionNote}",
            "PROPOSAL_NEEDS_REVISION");

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Proposal", proposalId, "RequestRevision",
            reviewerId, null, null, null, $"Yêu cầu bổ sung hồ sơ: {proposal.ClubName} - {req.RevisionNote}");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ProposalDetailDto?> GetByIdAsync(Guid proposalId)
    {
        var p = await _uow.Proposals.GetWithSubmitterAsync(proposalId);

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
        var query = _uow.Proposals.QueryAll();

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
        return await _uow.Proposals.QueryMyProposals(userId)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    private static ProposalDto MapToDto(ClubProposal p) => new(
        p.Id, p.ClubName, p.Category.ToString(), p.Description,
        p.Mission, p.Status.ToString(), p.FounderInfo,
        p.FounderStudentCode, p.ContactEmail, p.RejectionReason,
        p.SubmittedAt, p.ReviewedAt);
}
