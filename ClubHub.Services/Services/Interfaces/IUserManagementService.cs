using ClubHub.API.DTOs.Auth;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Enums;

namespace ClubHub.API.Services.Interfaces;

/// <summary>Quản lý người dùng (dành cho University Admin)</summary>
public interface IUserManagementService
{
    Task<PagedResult<UserProfileDto>> GetUsersAsync(Role? role, int page, int pageSize);
    Task<ApiResult<bool>> UpdateUserStatusAsync(Guid userId, UserStatus status);
}
