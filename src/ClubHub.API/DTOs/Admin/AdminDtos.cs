namespace ClubHub.API.DTOs.Admin;

public record UniversityStatisticsDto(
    int TotalUsers,
    int TotalStudents,
    int TotalUniversityAdmins,
    int TotalClubs,
    int ActiveClubs,
    int HiddenClubs,
    int LockedClubs,
    int DeletedClubs,
    int TotalEvents,
    int TotalClubMembers,
    int TotalClubProposals,
    int ApprovedClubProposals,
    int RejectedClubProposals,
    int TotalJoinRequests,
    int PendingJoinRequests,
    int PendingClubProposals
);

public record ClubStatisticsDto(
    Guid ClubId,
    string ClubName,
    int MemberCount,
    int PendingJoinRequests,
    int ApprovedJoinRequests,
    int EventCount,
    int UpcomingEventCount,
    int CompletedEventCount,
    int FeedbackCount,
    double AverageFeedbackRating,
    int PointTransactionCount,
    int TotalAwardedPoints
);
