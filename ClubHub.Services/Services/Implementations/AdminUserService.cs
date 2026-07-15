using ClubHub.API.DTOs.Admin;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public AdminUserService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilterRequest filter)
    {
        var query = _uow.Users.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) ||
                u.Username.Contains(term) ||
                u.Email.Contains(term) ||
                (u.StudentCode != null && u.StudentCode.Contains(term)));
        }

        if (filter.Role.HasValue) query = query.Where(u => u.SystemRole == filter.Role.Value);
        if (filter.IsActive.HasValue) query = query.Where(u => u.IsActive == filter.IsActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.FullName,
                u.Username,
                u.Email,
                u.StudentCode,
                u.Phone,
                u.AvatarUrl,
                u.SystemRole.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync();

        return new PagedResult<AdminUserDto>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ApiResult<AdminUserDto>> SetLockAsync(
        Guid userId, Guid requesterId, SetUserLockRequest request)
    {
        var isLocked = request.IsLocked!.Value;
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
            return ApiResult<AdminUserDto>.Failure("Người dùng không tồn tại.");
        if (userId == requesterId && isLocked)
            return ApiResult<AdminUserDto>.Failure(
                "Bạn không thể khóa chính tài khoản đang đăng nhập.");

        if (isLocked && user.IsActive && user.SystemRole == SystemRole.UniversityAdmin)
        {
            var activeAdminCount = await _uow.Users.CountAsync(u =>
                u.SystemRole == SystemRole.UniversityAdmin && u.IsActive);
            if (activeAdminCount <= 1)
                return ApiResult<AdminUserDto>.Failure(
                    "Không thể khóa University Admin đang hoạt động cuối cùng.");
        }

        user.IsActive = !isLocked;
        user.UpdatedAt = DateTime.UtcNow;
        if (isLocked)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
        }

        await _uow.SaveChangesAsync();
        await _auditService.LogAsync(
            "User",
            user.Id,
            isLocked ? "Lock" : "Unlock",
            requesterId,
            null,
            null,
            null,
            isLocked ? $"Khóa người dùng {user.FullName}" : $"Mở khóa người dùng {user.FullName}");

        return ApiResult<AdminUserDto>.Success(Map(user));
    }

    public async Task<ApiResult<AdminUserDto>> UpdateRoleAsync(
        Guid userId, Guid requesterId, UpdateUserRoleRequest request)
    {
        var newRole = request.Role!.Value;
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null)
            return ApiResult<AdminUserDto>.Failure("Người dùng không tồn tại.");

        if (user.IsActive && user.SystemRole == SystemRole.UniversityAdmin &&
            newRole != SystemRole.UniversityAdmin)
        {
            var activeAdminCount = await _uow.Users.CountAsync(u =>
                u.SystemRole == SystemRole.UniversityAdmin && u.IsActive);
            if (activeAdminCount <= 1)
                return ApiResult<AdminUserDto>.Failure(
                    "Không thể hạ quyền University Admin đang hoạt động cuối cùng.");
        }

        var previousRole = user.SystemRole;
        user.SystemRole = newRole;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync(
            "User",
            user.Id,
            "ChangeSystemRole",
            requesterId,
            null,
            null,
            $"{{\"oldRole\":\"{previousRole}\",\"newRole\":\"{newRole}\"}}",
            $"Đổi quyền hệ thống của {user.FullName}: {previousRole} -> {newRole}");

        return ApiResult<AdminUserDto>.Success(Map(user));
    }

    private static AdminUserDto Map(User u) => new(
        u.Id,
        u.FullName,
        u.Username,
        u.Email,
        u.StudentCode,
        u.Phone,
        u.AvatarUrl,
        u.SystemRole.ToString(),
        u.IsActive,
        u.CreatedAt,
        u.UpdatedAt);
}
