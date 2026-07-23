using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Auth;

public record RegisterRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, MaxLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? StudentCode,
    string? Phone
);

/// <summary>Xác thực OTP gửi qua email (dùng cho đăng ký và quên mật khẩu)</summary>
public record VerifyOtpRequest(
    [Required, EmailAddress] string Email,
    [Required, Length(6, 6)] string Otp
);

public record LoginRequest(
    [Required] string EmailOrUsername,
    [Required] string Password
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserProfileDto Profile
);

public record RefreshTokenRequest([Required] string RefreshToken);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword
);

public record ForgotPasswordRequest([Required, EmailAddress] string Email);

/// <summary>Đặt lại mật khẩu bằng OTP 6 chữ số gửi qua email</summary>
public record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required, Length(6, 6)] string Otp,
    [Required, MinLength(6)] string NewPassword
);

public record UpdateProfileRequest(
    [MaxLength(100)] string? FullName,
    [MaxLength(20)] string? Phone,
    string? AvatarUrl,
    string? CoverUrl
);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? StudentCode,
    string? Phone,
    string? AvatarUrl,
    string? CoverUrl,
    string Role,
    string Status,
    bool IsEmailVerified,
    DateTime CreatedAt
);

public record UpdateUserStatusRequest([Required] UserStatus Status);
