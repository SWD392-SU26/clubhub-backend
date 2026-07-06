using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class Club
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public ClubCategory Category { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public ClubStatus Status { get; set; } = ClubStatus.Active;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User Creator { get; set; } = null!;
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
