using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IClubRepository Clubs { get; }
    IClubMemberRepository ClubMembers { get; }
    IProposalRepository Proposals { get; }
    IEventRepository Events { get; }
    IEventRegistrationRepository EventRegistrations { get; }
    IGenericRepository<Feedback> Feedbacks { get; }
    IGenericRepository<PointTransaction> PointTransactions { get; }
    IGenericRepository<Notification> Notifications { get; }

    Task<int> SaveChangesAsync();
}
