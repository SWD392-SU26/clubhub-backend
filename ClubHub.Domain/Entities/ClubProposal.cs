using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class ClubProposal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string ClubName { get; set; } = string.Empty;

    public ClubCategory Category { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Mission { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? ActivityPlan { get; set; }

    [MaxLength(200)]
    public string FounderInfo { get; set; } = string.Empty;

    [MaxLength(20)]
    public string FounderStudentCode { get; set; } = string.Empty;

    public string? FounderIdCardUrl { get; set; }

    [MaxLength(150), EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(200)]
    public string? Advisor { get; set; }

    public string? LogoUrl { get; set; }
    public string? ProposalFileUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string? RejectionReason { get; set; }

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    public Guid SubmittedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation
    public User Submitter { get; set; } = null!;
    public User? Reviewer { get; set; }
}
