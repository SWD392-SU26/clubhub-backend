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
    [Required, MaxLength(200)] string FounderInfo,
    [Required, MaxLength(20)] string FounderStudentCode,
    string? FounderIdCardUrl,
    [Required, EmailAddress] string ContactEmail,
    string? ContactPhone,
    string? Advisor,
    string? LogoUrl,
    string? ProposalFileUrl,
    string? Notes
);

public record ReviewProposalRequest(
    [Required] bool IsApproved,
    string? RejectionReason
);

public record RequestRevisionRequest(
    [Required] string RevisionNote
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record ProposalDto(
    Guid Id,
    string ClubName,
    string Category,
    string? Description,
    string? Mission,
    string Status,
    string FounderInfo,
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
    string FounderInfo,
    string FounderStudentCode,
    string? FounderIdCardUrl,
    string ContactEmail,
    string? ContactPhone,
    string? Advisor,
    string? LogoUrl,
    string? ProposalFileUrl,
    string? Notes,
    string? RejectionReason,
    string Status,
    string SubmitterName,
    DateTime SubmittedAt,
    DateTime? ReviewedAt
);
