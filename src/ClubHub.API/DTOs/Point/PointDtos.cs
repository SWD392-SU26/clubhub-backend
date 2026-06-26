using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Point;

public record CreatePointTransactionRequest(
    [Required] Guid UserId,
    [Range(-1000, 1000)] int Points,
    [Required] PointType Type,
    [MaxLength(300)] string? Note,
    Guid? ReferenceId
);

public record PointTransactionDto(
    Guid Id,
    int Points,
    string Type,
    string? Note,
    DateTime CreatedAt
);

public record MemberPointDto(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    int TotalPoints,
    int Rank
);

public record MyPointSummaryDto(
    Guid ClubId,
    string ClubName,
    int TotalPoints,
    int Rank,
    List<PointTransactionDto> RecentTransactions
);
