using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Activity;

// ═════════════════════════════════════════════════════════════════════════════
// Request DTOs
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Tạo hoạt động nội bộ mới</summary>
public record CreateActivityRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,

    /// <summary>Loại hoạt động tự do (vd: Sinh hoạt, Từ thiện, Đóng góp, Dự án, ...)</summary>
    [Required, MaxLength(100)] string Type,

    string? Location,
    string? ImageUrl,

    [Required] DateTime StartTime,
    [Required] DateTime EndTime,
    DateTime? RegistrationDeadline,
    int? Capacity,
    int CheckInPoints = 10
);

/// <summary>Cập nhật hoạt động nội bộ</summary>
public record UpdateActivityRequest(
    [MaxLength(200)] string? Title,
    string? Description,
    [MaxLength(100)] string? Type,
    string? Location,
    string? ImageUrl,
    DateTime? StartTime,
    DateTime? EndTime,
    DateTime? RegistrationDeadline,
    int? Capacity,
    int? CheckInPoints,
    ActivityStatus? Status
);

/// <summary>Đăng ký tham gia hoạt động</summary>
public record RegisterActivityRequest(
    string? Note
);

// ═════════════════════════════════════════════════════════════════════════════
// Response DTOs
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Hoạt động nội bộ (danh sách)</summary>
public record ActivityDto(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Title,
    string? Description,
    string Type,
    string? Location,
    string? ImageUrl,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? RegistrationDeadline,
    int? Capacity,
    int RegisteredCount,
    string Status,
    string? CreatorName,
    DateTime CreatedAt
);

/// <summary>Hoạt động nội bộ (chi tiết — kèm danh sách người đăng ký)</summary>
public record ActivityDetailDto(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Title,
    string? Description,
    string Type,
    string? Location,
    string? ImageUrl,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? RegistrationDeadline,
    int? Capacity,
    int RegisteredCount,
    int CheckedInCount,
    string Status,
    int CheckInPoints,
    string? CreatorName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,

    /// <summary>Danh sách người đã đăng ký (chỉ ClubAdmin mới thấy)</summary>
    List<ActivityRegistrantDto>? Registrants
);

/// <summary>Người đăng ký tham gia hoạt động</summary>
public record ActivityRegistrantDto(
    Guid RegistrationId,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? StudentCode,
    bool IsCheckedIn,
    DateTime? CheckInTime,
    string? Note,
    DateTime RegisteredAt
);

/// <summary>Hoạt động đã đăng ký (dành cho Member xem lại)</summary>
public record MyRegisteredActivityDto(
    Guid RegistrationId,
    Guid ActivityId,
    Guid ClubId,
    string ClubName,
    string Title,
    string Type,
    string? Location,
    DateTime StartTime,
    DateTime EndTime,
    bool IsCheckedIn,
    DateTime? CheckInTime,
    string Status,
    DateTime RegisteredAt
);
