using ClubHub.API.DTOs.Auth;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class UserManagementService : IUserManagementService
{
    private readonly IUnitOfWork _uow;

    public UserManagementService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<UserProfileDto>> GetUsersAsync(Role? role, int page, int pageSize)
    {
        var query = _uow.Users.Query()
            .Where(u => u.Role != Role.UniversityAdmin); // Không liệt kê UniversityAdmin

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserProfileDto(
                u.Id, u.FullName, u.Username, u.Email,
                u.StudentCode, u.Phone, u.AvatarUrl, u.CoverUrl,
                u.Role.ToString(), u.Status.ToString(), u.IsEmailVerified, u.CreatedAt))
            .ToListAsync();

        return new PagedResult<UserProfileDto>(items, page, pageSize, total);
    }

    public async Task<ApiResult<bool>> UpdateUserStatusAsync(Guid userId, UserStatus status)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
            return ApiResult<bool>.Failure("Người dùng không tồn tại.");

        if (user.Role == Role.UniversityAdmin)
            return ApiResult<bool>.Failure("Không thể thay đổi trạng thái tài khoản Admin.");

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }
}
