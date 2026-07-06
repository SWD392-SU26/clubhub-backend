using ClubHub.API.Data;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context) { }

    public IQueryable<Event> QueryClubEvents(Guid clubId)
        => _dbSet
            .Include(e => e.Club)
            .Where(e => e.ClubId == clubId && e.Status != EventStatus.Draft)
            .OrderByDescending(e => e.StartTime);

    public async Task<Event?> GetEventWithClubAsync(Guid eventId)
        => await _dbSet
            .Include(e => e.Club)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

    public async Task<Event?> GetEventWithRegistrationsAsync(Guid eventId)
        => await _dbSet
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);
}
