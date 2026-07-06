namespace ClubHub.API.DTOs.Point;

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
