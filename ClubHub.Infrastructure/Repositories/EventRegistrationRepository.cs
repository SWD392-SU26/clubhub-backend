using ClubHub.API.Data;
using ClubHub.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class EventRegistrationRepository : GenericRepository<EventRegistration>, IEventRegistrationRepository
{
    public EventRegistrationRepository(AppDbContext context) : base(context) { }

    public async Task<EventRegistration?> GetByEventAndUserAsync(Guid eventId, Guid userId)
        => await _dbSet.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == userId && !r.IsCancelled);

    public IQueryable<EventRegistration> QueryMyRegistrations(Guid userId)
        => _dbSet
            .Include(r => r.Event)
            .Where(r => r.UserId == userId && !r.IsCancelled)
            .OrderByDescending(r => r.RegisteredAt);

    public IQueryable<EventRegistration> QueryEventRegistrations(Guid eventId)
        => _dbSet
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.RegisteredAt);
}
