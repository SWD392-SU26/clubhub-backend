using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class ClubMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid ClubId { get; set; }

    public ClubRole RoleInClub { get; set; } = ClubRole.Member;
    public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

    public string? JoinReason { get; set; }
    public string? RejectionReason { get; set; }

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Club Club { get; set; } = null!;
}
