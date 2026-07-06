using ClubHub.API.DTOs.Auth;

namespace ClubHub.API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResult<LoginResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<LoginResponse>> RefreshTokenAsync(string refreshToken);
    Task<ApiResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<ApiResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResult<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResult<UserProfileDto>> GetProfileAsync(Guid userId);
    Task<ApiResult<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<ApiResult<bool>> LogoutAsync(Guid userId);
}

public record ApiResult<T>(bool IsSuccess, string? Error, T? Data)
{
    public static ApiResult<T> Success(T data) => new(true, null, data);
    public static ApiResult<T> Failure(string error) => new(false, error, default);
}
