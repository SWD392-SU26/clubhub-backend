namespace ClubHub.API.Enums;

public enum SystemRole
{
    Student,
    UniversityAdmin
}

public enum ClubRole
{
    Member,
    VicePresident,
    President,
    ClubAdmin
}

public enum ClubStatus
{
    Active,
    Hidden,
    Locked,
    Archived,
    Deleted,
    Dissolved
}

public enum ClubCategory
{
    Academic,
    Technology,
    Sports,
    Arts,
    Volunteer,
    SoftSkills,
    Media,
    Entrepreneurship
}

public enum MembershipStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Left
}

public enum ProposalStatus
{
    Pending,
    Approved,
    Rejected,
    NeedsRevision
}

public enum EventStatus
{
    Draft,
    Published,
    Ongoing,
    Completed,
    Cancelled
}

public enum PointType
{
    CheckIn,
    Activity,
    Support,
    Feedback,
    Absence,
    Bonus,
    Penalty
}

public enum RequestStatus
{
    Pending,
    Approve,
    Rejected
}