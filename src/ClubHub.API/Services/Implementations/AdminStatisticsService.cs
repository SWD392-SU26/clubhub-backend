using ClubHub.API.Data;
using ClubHub.API.DTOs.Admin;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class AdminStatisticsService : IAdminStatisticsService
{
    private readonly AppDbContext _db;

    public AdminStatisticsService(AppDbContext db) => _db = db;

    public async Task<UniversityStatisticsDto> GetUniversityStatisticsAsync()
    {
        return new UniversityStatisticsDto(
            TotalUsers: await _db.Users.CountAsync(),
            TotalStudents: await _db.Users.CountAsync(u => u.SystemRole == SystemRole.Student),
            TotalUniversityAdmins: await _db.Users.CountAsync(u => u.SystemRole == SystemRole.UniversityAdmin),
            TotalClubs: await _db.Clubs.CountAsync(),
            ActiveClubs: await _db.Clubs.CountAsync(c => c.Status == ClubStatus.Active),
            HiddenClubs: await _db.Clubs.CountAsync(c => c.Status == ClubStatus.Hidden),
            LockedClubs: await _db.Clubs.CountAsync(c => c.Status == ClubStatus.Locked),
            DeletedClubs: await _db.Clubs.CountAsync(c => c.Status == ClubStatus.Deleted),
            TotalEvents: await _db.Events.CountAsync(),
            TotalClubMembers: await _db.ClubMembers.CountAsync(m => m.Status == MembershipStatus.Approved),
            TotalClubProposals: await _db.ClubProposals.CountAsync(),
            ApprovedClubProposals: await _db.ClubProposals.CountAsync(p => p.Status == ProposalStatus.Approved),
            RejectedClubProposals: await _db.ClubProposals.CountAsync(p => p.Status == ProposalStatus.Rejected),
            TotalJoinRequests: await _db.ClubMembers.CountAsync(),
            PendingJoinRequests: await _db.ClubMembers.CountAsync(m => m.Status == MembershipStatus.Pending),
            PendingClubProposals: await _db.ClubProposals.CountAsync(p => p.Status == ProposalStatus.Pending));
    }

    public async Task<ClubStatisticsDto?> GetClubStatisticsAsync(Guid clubId)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return null;

        return new ClubStatisticsDto(
            ClubId: club.Id,
            ClubName: club.Name,
            MemberCount: await _db.ClubMembers.CountAsync(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved),
            PendingJoinRequests: await _db.ClubMembers.CountAsync(m => m.ClubId == clubId && m.Status == MembershipStatus.Pending),
            ApprovedJoinRequests: await _db.ClubMembers.CountAsync(m => m.ClubId == clubId && m.Status == MembershipStatus.Approved),
            EventCount: await _db.Events.CountAsync(e => e.ClubId == clubId),
            UpcomingEventCount: await _db.Events.CountAsync(e => e.ClubId == clubId && e.StartTime >= DateTime.UtcNow && e.Status == EventStatus.Published),
            CompletedEventCount: await _db.Events.CountAsync(e => e.ClubId == clubId && e.Status == EventStatus.Completed),
            FeedbackCount: await _db.Feedbacks.CountAsync(f => f.Event.ClubId == clubId),
            AverageFeedbackRating: Math.Round(await _db.Feedbacks.Where(f => f.Event.ClubId == clubId).AverageAsync(f => (double?)f.Rating) ?? 0, 2),
            PointTransactionCount: await _db.PointTransactions.CountAsync(p => p.ClubId == clubId),
            TotalAwardedPoints: await _db.PointTransactions.Where(p => p.ClubId == clubId).SumAsync(p => (int?)p.Points) ?? 0);
    }

    public async Task<ApiResult<ClubStatisticsDto>> GetClubStatisticsForUserAsync(Guid clubId, Guid requesterId)
    {
        if (!await CanViewClubStatisticsAsync(clubId, requesterId))
            return ApiResult<ClubStatisticsDto>.Failure("You do not have permission to view this club's statistics.");

        var statistics = await GetClubStatisticsAsync(clubId);
        return statistics == null
            ? ApiResult<ClubStatisticsDto>.Failure("Club does not exist.")
            : ApiResult<ClubStatisticsDto>.Success(statistics);
    }

    private async Task<bool> CanViewClubStatisticsAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId &&
               m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));
}
