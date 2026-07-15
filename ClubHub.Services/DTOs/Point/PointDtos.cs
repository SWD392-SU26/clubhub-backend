using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Point;

public record AdjustPointsRequest(
    Guid UserId,
    PointType Type,
    int? Points,
    [MaxLength(300)] string? Note,
    Guid? ReferenceId
);

public record PointHistoryFilterRequest
{
    public Guid? UserId { get; init; }
    public PointType? Type { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

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

public record PointHistoryDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? StudentCode,
    int Points,
    string Type,
    string? Note,
    Guid? ReferenceId,
    Guid? AdjustedByUserId,
    string? AdjustedByName,
    DateTime CreatedAt
);
