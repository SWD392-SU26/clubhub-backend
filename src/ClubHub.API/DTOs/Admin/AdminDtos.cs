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
    int PendingJoinRequests,
    int PendingClubProposals
);

public record ClubStatisticsDto(
    Guid ClubId,
    string ClubName,
    int MemberCount,
    int PendingJoinRequests,
    int EventCount,
    int CompletedEventCount,
    int FeedbackCount,
    int TotalAwardedPoints
);
