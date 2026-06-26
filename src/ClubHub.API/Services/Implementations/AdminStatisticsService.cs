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
            EventCount: await _db.Events.CountAsync(e => e.ClubId == clubId),
            CompletedEventCount: await _db.Events.CountAsync(e => e.ClubId == clubId && e.Status == EventStatus.Completed),
            FeedbackCount: await _db.Feedbacks.CountAsync(f => f.Event.ClubId == clubId),
            TotalAwardedPoints: await _db.PointTransactions.Where(p => p.ClubId == clubId).SumAsync(p => (int?)p.Points) ?? 0);
    }
}
