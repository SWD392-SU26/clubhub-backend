using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Membership;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record JoinClubRequest(string? JoinReason);

public record ReviewMembershipRequest(
    [Required] bool IsApproved,
    string? RejectionReason
);

public record AssignRoleRequest(
    [Required] Guid UserId,
    [Required] ClubRole NewRole
);

public record TransferAdminRequest([Required] Guid NewAdminUserId);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record MembershipRequestDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? StudentCode,
    string JoinReason,
    string Status,
    DateTime RequestedAt
);

public record ClubMemberDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? StudentCode,
    string RoleInClub,
    DateTime JoinedAt
);

public record MyMembershipDto(
    Guid ClubId,
    string ClubName,
    string? ClubLogo,
    string RoleInClub,
    string Status,
    DateTime RequestedAt,
    DateTime? JoinedAt
);
