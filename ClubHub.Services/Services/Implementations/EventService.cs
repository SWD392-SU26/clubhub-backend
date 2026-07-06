using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Event;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;
    private readonly IPointService _pointService;

    public EventService(IUnitOfWork uow, IPointService pointService)
    {
        _uow = uow;
        _pointService = pointService;
    }

    public async Task<PagedResult<EventDto>> GetClubEventsAsync(Guid clubId, int page, int pageSize)
    {
        var query = _uow.Events.QueryClubEvents(clubId);

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
        var ev = await _uow.Events.GetEventWithClubAsync(eventId);

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

        _uow.Events.Add(ev);
        await _uow.SaveChangesAsync();

        var result = await GetEventByIdAsync(ev.Id);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest req, Guid requesterId)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
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

        await _uow.SaveChangesAsync();
        var result = await GetEventByIdAsync(eventId);
        return ApiResult<EventDto>.Success(result!);
    }

    public async Task<ApiResult<bool>> DeleteEventAsync(Guid eventId, Guid requesterId)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa sự kiện này.");

        ev.Status = EventStatus.Cancelled;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> RegisterForEventAsync(Guid eventId, Guid userId)
    {
        var ev = await _uow.Events.GetEventWithRegistrationsAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");
        if (ev.Status != EventStatus.Published) return ApiResult<bool>.Failure("Sự kiện không còn nhận đăng ký.");

        if (ev.Capacity.HasValue && ev.Registrations.Count(r => !r.IsCancelled) >= ev.Capacity.Value)
            return ApiResult<bool>.Failure("Sự kiện đã đủ số lượng tham gia.");

        if (ev.Registrations.Any(r => r.UserId == userId && !r.IsCancelled))
            return ApiResult<bool>.Failure("Bạn đã đăng ký sự kiện này rồi.");

        _uow.EventRegistrations.Add(new EventRegistration { EventId = eventId, UserId = userId });
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CancelRegistrationAsync(Guid eventId, Guid userId)
    {
        var reg = await _uow.EventRegistrations.GetByEventAndUserAsync(eventId, userId);

        if (reg == null) return ApiResult<bool>.Failure("Bạn chưa đăng ký sự kiện này.");

        reg.IsCancelled = true;
        reg.CancelledAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CheckInAsync(Guid eventId, Guid userId, Guid requesterId)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) return ApiResult<bool>.Failure("Sự kiện không tồn tại.");

        if (!await IsClubAdminAsync(ev.ClubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền check-in thành viên.");

        var reg = await _uow.EventRegistrations.GetByEventAndUserAsync(eventId, userId);

        if (reg == null) return ApiResult<bool>.Failure("Người dùng chưa đăng ký sự kiện.");
        if (reg.IsCheckedIn) return ApiResult<bool>.Failure("Người dùng đã check-in rồi.");

        reg.IsCheckedIn = true;
        reg.CheckInTime = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        // Award points
        await _pointService.AddPointsAsync(userId, ev.ClubId, 10, PointType.CheckIn,
            $"Check-in sự kiện: {ev.Name}", eventId);

        return ApiResult<bool>.Success(true);
    }

    public async Task<List<EventRegistrationDto>> GetMyRegistrationsAsync(Guid userId)
    {
        return await _uow.EventRegistrations.QueryMyRegistrations(userId)
            .Select(r => new EventRegistrationDto(
                r.Id, r.EventId, r.Event.Name,
                r.IsCheckedIn, r.CheckInTime, r.RegisteredAt))
            .ToListAsync();
    }

    public async Task<PagedResult<EventRegistrationDto>> GetEventRegistrationsAsync(Guid eventId, int page, int pageSize)
    {
        var query = _uow.EventRegistrations.QueryEventRegistrations(eventId);

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
        => await _uow.ClubMembers.IsClubAdminAsync(clubId, userId);

    private async Task<bool> IsMemberAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.IsMemberAsync(clubId, userId);

    private static EventDto MapToDto(Event e) => new(
        e.Id, e.ClubId, e.Club?.Name ?? "", e.Name, e.Description,
        e.Location, e.StartTime, e.EndTime, e.Capacity,
        e.Registrations?.Count(r => !r.IsCancelled) ?? 0,
        e.Status.ToString(), e.CreatedAt);
}
