using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.DTOs.Auth;

public record RegisterRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, MaxLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? StudentCode,
    string? Phone
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

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword
);

public record UpdateProfileRequest(
    [MaxLength(100)] string? FullName,
    [MaxLength(20)] string? Phone,
    string? AvatarUrl
);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? StudentCode,
    string? Phone,
    string? AvatarUrl,
    string SystemRole,
    DateTime CreatedAt
);
