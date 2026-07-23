using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? StudentCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>Ảnh bìa (cover photo) giống Facebook</summary>
    public string? CoverUrl { get; set; }

    /// <summary>Vai trò hợp nhất: Student, ClubMember, ClubAdmin, UniversityAdmin</summary>
    public Role Role { get; set; } = Role.Student;

    /// <summary>Trạng thái tài khoản: Active, Inactive, Lock, Deleted</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool IsEmailVerified { get; set; } = false;

    // OTP fields for email verification on register
    public string? EmailVerifyOtp { get; set; }
    public DateTime? EmailVerifyOtpExpiry { get; set; }

    // OTP fields for forgot password
    public string? PasswordResetOtp { get; set; }
    public DateTime? PasswordResetOtpExpiry { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ClubMember> ClubMemberships { get; set; } = new List<ClubMember>();
    public ICollection<ClubProposal> Proposals { get; set; } = new List<ClubProposal>();
    public ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ActivityRegistration> ActivityRegistrations { get; set; } = new List<ActivityRegistration>();
}
