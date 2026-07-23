using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class ClubActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClubId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Loại hoạt động tự do do người tạo định nghĩa (vd: Sinh hoạt, Từ thiện, Đóng góp, Dự án, ...)</summary>
    [MaxLength(100)]
    public string Type { get; set; } = "Sinh hoạt";

    [MaxLength(300)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>Hạn chót đăng ký tham gia. NULL = không giới hạn.</summary>
    public DateTime? RegistrationDeadline { get; set; }

    /// <summary>Số lượng tối đa người tham gia. NULL = không giới hạn.</summary>
    public int? Capacity { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Upcoming;

    /// <summary>Điểm thưởng khi check-in thành công (mặc định 10)</summary>
    public int CheckInPoints { get; set; } = 10;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Club Club { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
}
