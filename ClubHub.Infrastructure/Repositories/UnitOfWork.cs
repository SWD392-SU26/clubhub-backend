using ClubHub.API.Data;
using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IUserRepository? _users;
    private IClubRepository? _clubs;
    private IClubMemberRepository? _clubMembers;
    private IProposalRepository? _proposals;
    private IEventRepository? _events;
    private IEventRegistrationRepository? _eventRegistrations;
    private IGenericRepository<Feedback>? _feedbacks;
    private IGenericRepository<PointTransaction>? _pointTransactions;
    private IGenericRepository<Notification>? _notifications;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IClubRepository Clubs => _clubs ??= new ClubRepository(_context);
    public IClubMemberRepository ClubMembers => _clubMembers ??= new ClubMemberRepository(_context);
    public IProposalRepository Proposals => _proposals ??= new ProposalRepository(_context);
    public IEventRepository Events => _events ??= new EventRepository(_context);
    public IEventRegistrationRepository EventRegistrations => _eventRegistrations ??= new EventRegistrationRepository(_context);
    public IGenericRepository<Feedback> Feedbacks => _feedbacks ??= new GenericRepository<Feedback>(_context);
    public IGenericRepository<PointTransaction> PointTransactions => _pointTransactions ??= new GenericRepository<PointTransaction>(_context);
    public IGenericRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
