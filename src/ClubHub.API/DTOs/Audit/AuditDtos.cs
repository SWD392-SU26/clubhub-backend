namespace ClubHub.API.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    Guid ClubId,
    Guid? ActorUserId,
    string? ActorName,
    Guid? TargetUserId,
    string? TargetName,
    string Action,
    string? TargetType,
    Guid? TargetId,
    string? Description,
    DateTime CreatedAt
);
