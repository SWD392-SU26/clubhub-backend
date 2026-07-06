using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.Entities;

public class ProposalRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProposalId { get; set; }

    public int RevisionNumber { get; set; }

    [MaxLength(150)]
    public string ClubName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

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

    [MaxLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(200)]
    public string? Advisor { get; set; }

    public string? LogoUrl { get; set; }
    public string? ProposalFileUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string? RevisionNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ClubProposal Proposal { get; set; } = null!;
}
