using ClubHub.API.Data;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Event;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class EventService : IEventService
{
    private readonly AppDbContext _db;
    private readonly IPointService _pointService;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;

    public EventService(
        AppDbContext db,
        IPointService pointService,
        INotificationService notificationService,
        IAuditLogService auditLogService)
    {
        _db = db;
        _pointService = pointService;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<EventDto>> GetClubEventsAsync(Guid clubId, int page, int pageSize)
    {
        var query = _db.Events
            .Include(e => e.Club)
            .Where(e => e.ClubId == clubId && e.Status != EventStatus.Draft)
            .OrderByDescending(e => e.StartTime);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return new PagedResult<EventDto>(items, page, pageSize, total);
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        var ev = await _db.Events
            .Include(e => e.Club)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        return ev == null ? null : MapToDto(ev);
    }

    public async Task<ApiResult<EventDto>> CreateEventAsync(Guid clubId, CreateEventRequest req, Guid createdBy)
    {
        if (!await IsMemberAsync(clubId, createdBy))
            return ApiResult<EventDto>.Failure("Bạn không phải thành viên CLB.");

        if (!await IsClubAdminAsync(clubId, createdBy))
            return ApiResult<EventDto>.Failure("Chỉ Club Admin/President mới được tạo sự kiện.");

        if (req.StartTime >= req.EndTime)
            return ApiResult<EventDto>.Failure("Thời gian kết thúc phải sau thời gian bắt đầu.");

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
        await _auditLogService.LogAsync(
            createdBy, "EVENT_CREATED", nameof(Event), ev.Id, clubId,
            details: ev.Name);

        // Thông báo cho tất cả thành viên CLB (trừ người tạo)
        var memberIds = await _db.ClubMembers
            .Where(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved && m.UserId != createdBy)
            .Select(m => m.UserId)
            .ToListAsync();

        if (memberIds.Count > 0)
            await _notificationService.CreateManyAsync(
                memberIds,
                "Sự kiện mới",
                $"CLB vừa tạo sự kiện mới: {ev.Name}.",
                "EVENT_CREATED");

        var result = await GetEventByIdAsync(ev.Id);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest req, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<EventDto>.Failure("Sự kiện không tồn tại.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<EventDto>.Failure("Bạn không có quyền chỉnh sửa sự kiện này.");

        if (req.Name != null) ev.Name = req.Name;
        if (req.Description != null) ev.Description = req.Description;
        if (req.Location != null) ev.Location = req.Location;
        if (req.StartTime.HasValue) ev.StartTime = req.StartTime.Value;
        if (req.EndTime.HasValue) ev.EndTime = req.EndTime.Value;
        if (req.Capacity.HasValue) ev.Capacity = req.Capacity;
        if (req.Status.HasValue) ev.Status = req.Status.Value;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            requesterId, "EVENT_UPDATED", nameof(Event), ev.Id, ev.ClubId,
            details: ev.Name);
        var result = await GetEventByIdAsync(eventId);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<bool>> DeleteEventAsync(Guid eventId, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa sự kiện này.");

        ev.Status = EventStatus.Cancelled;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            requesterId, "EVENT_CANCELLED", nameof(Event), ev.Id, ev.ClubId,
            details: ev.Name);

        // Thông báo cho những người đã đăng ký sự kiện
        var registrantIds = await _db.EventRegistrations
            .Where(r => r.EventId == eventId && !r.IsCancelled)
            .Select(r => r.UserId)
            .ToListAsync();

        if (registrantIds.Count > 0)
            await _notificationService.CreateManyAsync(
                registrantIds,
                "Sự kiện đã bị hủy",
                $"Sự kiện {ev.Name} mà bạn đã đăng ký đã bị hủy.",
                "EVENT_CANCELLED");

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RegisterForEventAsync(Guid eventId, Guid userId)
    {
        var ev = await _db.Events.Include(e => e.Registrations).FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");
        if (ev.Status != EventStatus.Published) return ApiResult<bool>.Failure("Sự kiện không còn nhận đăng ký.");

        if (ev.Capacity.HasValue && ev.Registrations.Count(r => !r.IsCancelled) >= ev.Capacity.Value)
            return ApiResult<bool>.Failure("Sự kiện đã đủ số lượng tham gia.");

        if (ev.Registrations.Any(r => r.UserId == userId && !r.IsCancelled))
            return ApiResult<bool>.Failure("Bạn đã đăng ký sự kiện này rồi.");

        var registration = new EventRegistration { EventId = eventId, UserId = userId };
        _db.EventRegistrations.Add(registration);
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            userId, "EVENT_REGISTERED", nameof(EventRegistration), registration.Id,
            ev.ClubId, userId, ev.Name);
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CancelRegistrationAsync(Guid eventId, Guid userId)
    {
        var reg = await _db.EventRegistrations.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == userId && !r.IsCancelled);

        if (reg == null) return ApiResult<bool>.Failure("Bạn chưa đăng ký sự kiện này.");

        reg.IsCancelled = true;
        reg.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var clubId = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => e.ClubId)
            .FirstAsync();
        await _auditLogService.LogAsync(
            userId, "EVENT_REGISTRATION_CANCELLED", nameof(EventRegistration), reg.Id,
            clubId, userId);
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CheckInAsync(Guid eventId, Guid userId, Guid requesterId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền check-in thành viên.");

        var reg = await _db.EventRegistrations.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == userId && !r.IsCancelled);

        if (reg == null) return ApiResult<bool>.Failure("Người dùng chưa đăng ký sự kiện.");
        if (reg.IsCheckedIn) return ApiResult<bool>.Failure("Người dùng đã check-in rồi.");

        reg.IsCheckedIn = true;
        reg.CheckInTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Award points
        await _pointService.AddPointsAsync(userId, ev.ClubId, 10, PointType.CheckIn,
            $"Check-in sự kiện: {ev.Name}", eventId);

        await _auditLogService.LogAsync(
            requesterId, "EVENT_CHECKED_IN", nameof(EventRegistration), reg.Id,
            ev.ClubId, userId, ev.Name);

        return ApiResult<bool>.Success(true);
    }

    public async Task<List<MyEventDto>> GetMyRegistrationsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _db.EventRegistrations
            .Include(r => r.Event)
                .ThenInclude(e => e.Club)
            .Where(r => r.UserId == userId && !r.IsCancelled)
            .OrderByDescending(r => r.Event.StartTime)
            .Select(r => new MyEventDto(
                r.Id,
                r.EventId,
                r.Event.ClubId,
                r.Event.Club.Name,
                r.Event.Name,
                r.Event.Location,
                r.Event.StartTime,
                r.Event.EndTime,
                r.Event.Status.ToString(),
                r.IsCheckedIn,
                r.CheckInTime,
                r.RegisteredAt,
                r.Event.Status == EventStatus.Completed && r.IsCheckedIn &&
                    !r.Event.Feedbacks.Any(f => f.UserId == userId),
                r.Event.Feedbacks.Any(f => f.UserId == userId),
                r.Event.Status == EventStatus.Published && r.Event.StartTime > now))
            .ToListAsync();
    }

    public async Task<PagedResult<EventRegistrationDto>> GetEventRegistrationsAsync(Guid eventId, int page, int pageSize)
    {
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

    private async Task<bool> IsMemberAsync(Guid clubId, Guid userId)
        => await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

    private static EventDto MapToDto(Event e) => new(
        e.Id, e.ClubId, e.Club?.Name ?? "", e.Name, e.Description,
        e.Location, e.StartTime, e.EndTime, e.Capacity,
        e.Registrations?.Count(r => !r.IsCancelled) ?? 0,
        e.Status.ToString(), e.CreatedAt);
}
