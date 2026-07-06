using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IEventRepository : IGenericRepository<Event>
{
    IQueryable<Event> QueryClubEvents(Guid clubId);
    Task<Event?> GetEventWithClubAsync(Guid eventId);
    Task<Event?> GetEventWithRegistrationsAsync(Guid eventId);
}
