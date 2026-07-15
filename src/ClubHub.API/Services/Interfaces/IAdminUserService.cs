using ClubHub.API.DTOs.Admin;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilterRequest filter);
    Task<ApiResult<AdminUserDto>> SetLockAsync(
        Guid userId, Guid requesterId, SetUserLockRequest request);
    Task<ApiResult<AdminUserDto>> UpdateRoleAsync(
        Guid userId, Guid requesterId, UpdateUserRoleRequest request);
}
