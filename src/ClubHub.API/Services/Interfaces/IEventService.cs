using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Event;

namespace ClubHub.API.Services.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetPublicEventsAsync(EventFilterRequest filter, bool upcomingOnly);
    Task<PagedResult<EventDto>> GetClubEventsAsync(Guid clubId, int page, int pageSize);
    Task<EventDto?> GetEventByIdAsync(Guid eventId, Guid? requesterId = null);
    Task<ApiResult<EventDto>> CreateEventAsync(Guid clubId, CreateEventRequest request, Guid createdBy);
    Task<ApiResult<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest request, Guid requesterId);
    Task<ApiResult<bool>> DeleteEventAsync(Guid eventId, Guid requesterId);
    Task<ApiResult<bool>> RegisterForEventAsync(Guid eventId, Guid userId);
    Task<ApiResult<bool>> CancelRegistrationAsync(Guid eventId, Guid userId);
    Task<ApiResult<bool>> CheckInAsync(Guid eventId, Guid userId, Guid requesterId);
    Task<List<EventRegistrationDto>> GetMyRegistrationsAsync(Guid userId);
    Task<PagedResult<EventRegistrationDto>> GetEventRegistrationsAsync(Guid eventId, Guid requesterId, int page, int pageSize);
}
