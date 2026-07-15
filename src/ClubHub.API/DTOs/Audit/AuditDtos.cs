using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.DTOs.Audit;

public record AuditLogFilterRequest
{
    public Guid? ClubId { get; init; }
    public string? Action { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid? TargetUserId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public record AuditLogDto(
    Guid Id,
    Guid? ClubId,
    string? ClubName,
    Guid ActorUserId,
    string ActorName,
    Guid? TargetUserId,
    string? TargetUserName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Details,
    DateTime CreatedAt
);
