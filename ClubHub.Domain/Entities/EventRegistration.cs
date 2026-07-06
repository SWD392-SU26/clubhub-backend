namespace ClubHub.API.Entities;

public class EventRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    public bool IsCheckedIn { get; set; } = false;
    public DateTime? CheckInTime { get; set; }

    public bool IsCancelled { get; set; } = false;
    public DateTime? CancelledAt { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
}
