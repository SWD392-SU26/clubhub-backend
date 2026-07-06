using System.ComponentModel.DataAnnotations;
using ClubHub.API.Enums;

namespace ClubHub.API.DTOs.Event;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateEventRequest(
    [Required, MaxLength(200)] string Name,
    string? Description,
    string? Location,
    [Required] DateTime StartTime,
    [Required] DateTime EndTime,
    int? Capacity
);

public record UpdateEventRequest(
    [MaxLength(200)] string? Name,
    string? Description,
    string? Location,
    DateTime? StartTime,
    DateTime? EndTime,
    int? Capacity,
    EventStatus? Status
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record EventDto(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Name,
    string? Description,
    string? Location,
    DateTime StartTime,
    DateTime EndTime,
    int? Capacity,
    int RegisteredCount,
    string Status,
    DateTime CreatedAt
);

public record EventRegistrationDto(
    Guid Id,
    Guid EventId,
    string EventName,
    bool IsCheckedIn,
    DateTime? CheckInTime,
    DateTime RegisteredAt
);
