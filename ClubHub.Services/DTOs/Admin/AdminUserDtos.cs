using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Admin;

public record AdminUserFilterRequest
{
    public string? SearchTerm { get; init; }
    public SystemRole? Role { get; init; }
    public bool? IsActive { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public record SetUserLockRequest([Required] bool? IsLocked);

public record UpdateUserRoleRequest([Required] SystemRole? Role);

public record AdminUserDto(
    Guid Id,
    string FullName,
    string Username,
    string Email,
    string? StudentCode,
    string? Phone,
    string? AvatarUrl,
    string SystemRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
