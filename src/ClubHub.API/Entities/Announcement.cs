using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.Entities;

public class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClubId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Club Club { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
