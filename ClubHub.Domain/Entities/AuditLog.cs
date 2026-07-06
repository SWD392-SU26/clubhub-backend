using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tên entity bị tác động (Club, ClubMember, Event, Proposal, ...)</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID của entity bị tác động</summary>
    public Guid EntityId { get; set; }

    /// <summary>Hành động (Create, Update, Delete, Hide, Lock, AssignRole, ...)</summary>
    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>ID người thực hiện</summary>
    public Guid? PerformedBy { get; set; }

    /// <summary>Tên người thực hiện (denormalized để tra cứu nhanh)</summary>
    [MaxLength(100)]
    public string? PerformedByName { get; set; }

    /// <summary>ClubId liên quan (nếu có)</summary>
    public Guid? ClubId { get; set; }

    /// <summary>Chi tiết dạng JSON</summary>
    public string? Details { get; set; }

    /// <summary>Mô tả ngắn gọn bằng tiếng Việt</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Performer { get; set; }
}
