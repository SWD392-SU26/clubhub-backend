using ClubHub.API.Data;
using ClubHub.API.DTOs.Admin;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AdminUserService(AppDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilterRequest filter)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

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
            .Select(u => Map(u))
            .ToListAsync();

        return new PagedResult<AdminUserDto>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ApiResult<AdminUserDto>> SetLockAsync(
        Guid userId, Guid requesterId, SetUserLockRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult<AdminUserDto>.Failure("Người dùng không tồn tại.");
        if (userId == requesterId && request.IsLocked)
            return ApiResult<AdminUserDto>.Failure("Bạn không thể khóa chính tài khoản đang đăng nhập.");

        if (request.IsLocked && user.IsActive && user.SystemRole == SystemRole.UniversityAdmin)
        {
            var activeAdminCount = await _db.Users.CountAsync(u =>
                u.SystemRole == SystemRole.UniversityAdmin && u.IsActive);
            if (activeAdminCount <= 1)
                return ApiResult<AdminUserDto>.Failure(
                    "Không thể khóa University Admin đang hoạt động cuối cùng.");
        }

        user.IsActive = !request.IsLocked;
        user.UpdatedAt = DateTime.UtcNow;

        if (request.IsLocked)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
        }

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            requesterId,
            request.IsLocked ? "USER_LOCKED" : "USER_UNLOCKED",
            nameof(User),
            user.Id,
            targetUserId: user.Id);

        return ApiResult<AdminUserDto>.Success(Map(user));
    }

    public async Task<ApiResult<AdminUserDto>> UpdateRoleAsync(
        Guid userId, Guid requesterId, UpdateUserRoleRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult<AdminUserDto>.Failure("Người dùng không tồn tại.");

        if (user.IsActive &&
            user.SystemRole == SystemRole.UniversityAdmin &&
            request.Role != SystemRole.UniversityAdmin)
        {
            var activeAdminCount = await _db.Users.CountAsync(u =>
                u.SystemRole == SystemRole.UniversityAdmin && u.IsActive);
            if (activeAdminCount <= 1)
                return ApiResult<AdminUserDto>.Failure("Không thể hạ quyền University Admin đang hoạt động cuối cùng.");
        }

        var previousRole = user.SystemRole;
        user.SystemRole = request.Role;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync(
            requesterId,
            "SYSTEM_ROLE_CHANGED",
            nameof(User),
            user.Id,
            targetUserId: user.Id,
            details: $"{previousRole} -> {request.Role}");

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
