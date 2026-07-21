using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Club;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateClubRequest(
    [Required, MaxLength(150)] string Name,
    ClubCategory Category,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl
);

public record UpdateClubRequest(
    [MaxLength(150)] string? Name,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl
);

/// <summary>Dành cho UniversityAdmin tạo CLB trực tiếp và chọn ClubAdmin</summary>
public record CreateClubWithAdminRequest(
    [Required, MaxLength(150)] string Name,
    ClubCategory Category,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl,
    [Required] Guid ClubAdminUserId
);

public record UpdateClubStatusRequest([Required] ClubStatus Status);

public record ClubFilterRequest
{
    public ClubCategory? Category { get; init; }
    public string? SearchTerm { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record ClubSummaryDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl,
    string Status,
    int MemberCount,
    DateTime CreatedAt
);

public record ClubDetailDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string? LogoUrl,
    string? CoverImageUrl,
    string Status,
    int MemberCount,
    List<ClubOfficerDto> Officers,
    List<ClubMemberDetailDto> Members,
    DateTime CreatedAt
);

public record ClubOfficerDto(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string RoleInClub
);

public record ClubMemberDetailDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? StudentCode,
    string RoleInClub,
    DateTime? JoinedAt
);
