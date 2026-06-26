using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClubId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
    public User? ActorUser { get; set; }
    public User? TargetUser { get; set; }
}
