using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Event;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class EventService : IEventService
{
    private readonly AppDbContext _db;
    private readonly IPointService _pointService;
    private readonly IAuditLogService _auditLogService;

    public EventService(AppDbContext db, IPointService pointService, IAuditLogService auditLogService)
    {
        _db = db;
        _pointService = pointService;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<EventDto>> GetPublicEventsAsync(EventFilterRequest filter, bool upcomingOnly)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var now = DateTime.UtcNow;

        var query = _db.Events
            .Include(e => e.Club)
            .Include(e => e.Registrations)
            .Where(e => e.Status != EventStatus.Draft && e.Status != EventStatus.Cancelled);

        if (upcomingOnly)
            query = query.Where(e => e.StartTime >= now && e.Status == EventStatus.Published);

        if (filter.ClubId.HasValue)
            query = query.Where(e => e.ClubId == filter.ClubId.Value);

        if (filter.Category.HasValue)
            query = query.Where(e => e.Club.Category == filter.Category.Value);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(e =>
                e.Name.Contains(keyword) ||
                (e.Description != null && e.Description.Contains(keyword)) ||
                (e.Location != null && e.Location.Contains(keyword)) ||
                e.Club.Name.Contains(keyword));
        }

        if (filter.FromDate.HasValue)
            query = query.Where(e => e.StartTime >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(e => e.StartTime <= filter.ToDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return new PagedResult<EventDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<EventDto>> GetClubEventsAsync(Guid clubId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Events
            .Include(e => e.Club)
            .Include(e => e.Registrations)
            .Where(e => e.ClubId == clubId && e.Status != EventStatus.Draft && e.Status != EventStatus.Cancelled)
            .OrderByDescending(e => e.StartTime);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return new PagedResult<EventDto>(items, page, pageSize, total);
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId, Guid? requesterId = null)
    {
        var ev = await _db.Events
            .Include(e => e.Club)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null) return null;
        if (IsPublicEvent(ev)) return MapToDto(ev);

        if (!requesterId.HasValue)
            return null;

        if (!await CanViewPrivateEventAsync(ev.ClubId, requesterId.Value))
            return null;

        return MapToDto(ev);
    }

    public async Task<ApiResult<EventDto>> CreateEventAsync(Guid clubId, CreateEventRequest req, Guid createdBy)
    {
        if (!await IsClubAdminAsync(clubId, createdBy))
            return ApiResult<EventDto>.Failure("Only club admins, presidents, or vice presidents can create events.");

        if (req.StartTime >= req.EndTime)
            return ApiResult<EventDto>.Failure("End time must be after start time.");

        var ev = new Event
        {
            ClubId = clubId,
            Name = req.Name,
            Description = req.Description,
            Location = req.Location,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Capacity = req.Capacity,
            CreatedBy = createdBy,
            Status = EventStatus.Published
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, createdBy, "EventCreated", nameof(Event), ev.Id, description: $"Created event '{ev.Name}'.");

        var result = await GetEventByIdAsync(ev.Id, createdBy);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest req, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<EventDto>.Failure("Event does not exist.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<EventDto>.Failure("You do not have permission to update this event.");

        if (req.StartTime.HasValue && req.EndTime.HasValue && req.StartTime.Value >= req.EndTime.Value)
            return ApiResult<EventDto>.Failure("End time must be after start time.");

        if (req.Name != null) ev.Name = req.Name;
        if (req.Description != null) ev.Description = req.Description;
        if (req.Location != null) ev.Location = req.Location;
        if (req.StartTime.HasValue) ev.StartTime = req.StartTime.Value;
        if (req.EndTime.HasValue) ev.EndTime = req.EndTime.Value;
        if (req.Capacity.HasValue) ev.Capacity = req.Capacity;
        if (req.Status.HasValue) ev.Status = req.Status.Value;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(ev.ClubId, requesterId, "EventUpdated", nameof(Event), ev.Id, description: $"Updated event '{ev.Name}'.");

        var result = await GetEventByIdAsync(eventId, requesterId);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<bool>> DeleteEventAsync(Guid eventId, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Event does not exist.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("You do not have permission to cancel this event.");

        ev.Status = EventStatus.Cancelled;
        ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(ev.ClubId, requesterId, "EventCancelled", nameof(Event), ev.Id, description: $"Cancelled event '{ev.Name}'.");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RegisterForEventAsync(Guid eventId, Guid userId)
    {
        var ev = await _db.Events.Include(e => e.Registrations).FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return ApiResult<bool>.Failure("Event does not exist.");
        if (ev.Status != EventStatus.Published) return ApiResult<bool>.Failure("Event is not open for registration.");
        if (!await IsMemberAsync(ev.ClubId, userId))
            return ApiResult<bool>.Failure("Only approved club members can register for club events.");

        if (ev.Capacity.HasValue && ev.Registrations.Count(r => !r.IsCancelled) >= ev.Capacity.Value)
            return ApiResult<bool>.Failure("Event capacity has been reached.");

        if (ev.Registrations.Any(r => r.UserId == userId && !r.IsCancelled))
            return ApiResult<bool>.Failure("You already registered for this event.");

        var registration = new EventRegistration { EventId = eventId, UserId = userId };
        _db.EventRegistrations.Add(registration);
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(ev.ClubId, userId, "EventRegistered", nameof(EventRegistration), registration.Id, userId, $"Registered for event '{ev.Name}'.");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CancelRegistrationAsync(Guid eventId, Guid userId)
    {
        var reg = await _db.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && !r.IsCancelled);

        if (reg == null) return ApiResult<bool>.Failure("You have not registered for this event.");

        reg.IsCancelled = true;
        reg.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(reg.Event.ClubId, userId, "EventRegistrationCancelled", nameof(EventRegistration), reg.Id, userId);

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CheckInAsync(Guid eventId, Guid userId, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Event does not exist.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("You do not have permission to check in members.");

        var reg = await _db.EventRegistrations.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == userId && !r.IsCancelled);

        if (reg == null) return ApiResult<bool>.Failure("User has not registered for this event.");
        if (reg.IsCheckedIn) return ApiResult<bool>.Failure("User is already checked in.");

        reg.IsCheckedIn = true;
        reg.CheckInTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _pointService.AddPointsAsync(userId, ev.ClubId, 10, PointType.CheckIn, $"Check in event: {ev.Name}", eventId);
        await _auditLogService.LogAsync(ev.ClubId, requesterId, "EventCheckIn", nameof(EventRegistration), reg.Id, userId, $"Checked in to event '{ev.Name}'.");

        return ApiResult<bool>.Success(true);
    }

    public async Task<List<EventRegistrationDto>> GetMyRegistrationsAsync(Guid userId)
    {
        return await _db.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.UserId == userId && !r.IsCancelled)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => new EventRegistrationDto(
                r.Id, r.EventId, r.Event.Name,
                r.IsCheckedIn, r.CheckInTime, r.RegisteredAt))
            .ToListAsync();
    }

    public async Task<PagedResult<EventRegistrationDto>> GetEventRegistrationsAsync(Guid eventId, Guid requesterId, int page, int pageSize)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) throw new KeyNotFoundException("Event does not exist.");
        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            throw new UnauthorizedAccessException("You do not have permission to view event registrations.");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.RegisteredAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new EventRegistrationDto(
                r.Id, r.EventId, r.Event.Name,
                r.IsCheckedIn, r.CheckInTime, r.RegisteredAt))
            .ToListAsync();

        return new PagedResult<EventRegistrationDto>(items, page, pageSize, total);
    }

    private static bool IsPublicEvent(Event ev)
        => ev.Status != EventStatus.Draft && ev.Status != EventStatus.Cancelled;

    private async Task<bool> CanViewPrivateEventAsync(Guid clubId, Guid userId)
        => await IsMemberAsync(clubId, userId);

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private async Task<bool> IsMemberAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

    private static EventDto MapToDto(Event e) => new(
        e.Id, e.ClubId, e.Club?.Name ?? "", e.Name, e.Description,
        e.Location, e.StartTime, e.EndTime, e.Capacity,
        e.Registrations?.Count(r => !r.IsCancelled) ?? 0,
        e.Status.ToString(), e.CreatedAt);
}
