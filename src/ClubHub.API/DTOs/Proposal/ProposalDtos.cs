using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Proposal;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record SubmitProposalRequest(
    [Required, MaxLength(150)] string ClubName,
    [Required] ClubCategory Category,
    string? Description,
    string? Mission,
    string? Reason,
    string? ActivityPlan,
    [Required, MaxLength(200)] string FounderFullName,
    [Required, MaxLength(20)] string FounderStudentCode,
    string? FounderIdentityDocumentUrl,
    [Required, EmailAddress] string ContactEmail,
    string? ContactPhone,
    string? AdvisorName,
    string? LogoUrl,
    string? ProposalFileUrl,
    string? AdditionalNote
);

public record ReviewProposalRequest(
    [Required] bool IsApproved,
    string? RejectionReason
);

public record RequestRevisionRequest(
    [Required] string RevisionNote
);

public record UpdateProposalRequest(
    [MaxLength(150)] string? ClubName,
    ClubCategory? Category,
    string? Description,
    string? Mission,
    string? Reason,
    string? ActivityPlan,
    [MaxLength(200)] string? FounderFullName,
    [MaxLength(20)] string? FounderStudentCode,
    string? FounderIdentityDocumentUrl,
    [EmailAddress] string? ContactEmail,
    string? ContactPhone,
    string? AdvisorName,
    string? LogoUrl,
    string? ProposalFileUrl,
    string? AdditionalNote
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record ProposalDto(
    Guid Id,
    string ClubName,
    string Category,
    string? Description,
    string? Mission,
    string Status,
    string FounderFullName,
    string FounderStudentCode,
    string ContactEmail,
    string? RejectionReason,
    DateTime SubmittedAt,
    DateTime? ReviewedAt
);

public record ProposalDetailDto(
    Guid Id,
    string ClubName,
    string Category,
    string? Description,
    string? Mission,
    string? Reason,
    string? ActivityPlan,
    string FounderFullName,
    string FounderStudentCode,
    string? FounderIdentityDocumentUrl,
    string ContactEmail,
    string? ContactPhone,
    string? AdvisorName,
    string? LogoUrl,
    string? ProposalFileUrl,
    string? AdditionalNote,
    string? RejectionReason,
    string? RevisionNote,
    DateTime? RequestedRevisionAt,
    DateTime? ResubmittedAt,
    string Status,
    string SubmitterName,
    DateTime SubmittedAt,
    DateTime? ReviewedAt
);
