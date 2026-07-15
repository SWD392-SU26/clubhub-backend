using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ClubId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Club? Club { get; set; }
    public User Actor { get; set; } = null!;
    public User? TargetUser { get; set; }
}
