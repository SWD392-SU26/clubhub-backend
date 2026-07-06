using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClubId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int? Capacity { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Club Club { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
