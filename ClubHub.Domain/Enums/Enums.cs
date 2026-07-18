namespace ClubHub.API.Enums;

/// <summary>Vai trò hệ thống hợp nhất</summary>
public enum Role
{
    Student,
    ClubMember,
    ClubAdmin,
    UniversityAdmin
}

/// <summary>Trạng thái tài khoản người dùng</summary>
public enum UserStatus
{
    Active,
    Inactive,
    Lock,
    Deleted
}

public enum ClubStatus
{
    Active,
    Inactive,
    Lock,
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