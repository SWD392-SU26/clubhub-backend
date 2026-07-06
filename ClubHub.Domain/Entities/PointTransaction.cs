using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class PointTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid ClubId { get; set; }

    public int Points { get; set; }

    public PointType Type { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    // Reference to Event / EventRegistration if applicable
    public Guid? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Club Club { get; set; } = null!;
}
