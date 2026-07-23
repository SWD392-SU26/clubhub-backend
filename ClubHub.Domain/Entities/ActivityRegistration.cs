namespace ClubHub.API.Entities;

public class ActivityRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ActivityId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Lý do / ghi chú khi đăng ký</summary>
    public string? Note { get; set; }

    public bool IsCheckedIn { get; set; } = false;
    public DateTime? CheckInTime { get; set; }

    public bool IsCancelled { get; set; } = false;
    public DateTime? CancelledAt { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ClubActivity Activity { get; set; } = null!;
    public User User { get; set; } = null!;
}
