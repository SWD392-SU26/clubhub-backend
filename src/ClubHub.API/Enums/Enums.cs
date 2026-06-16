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
    Deleted
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
