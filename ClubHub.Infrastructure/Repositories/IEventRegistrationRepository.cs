using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IEventRegistrationRepository : IGenericRepository<EventRegistration>
{
    Task<EventRegistration?> GetByEventAndUserAsync(Guid eventId, Guid userId);
    IQueryable<EventRegistration> QueryMyRegistrations(Guid userId);
    IQueryable<EventRegistration> QueryEventRegistrations(Guid eventId);
}
