using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Data;

public static class DataSeeder
{
    public static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid StudentOneId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid StudentTwoId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid StudentThreeId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid TechClubId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid AcademicClubId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid SportsClubId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    public static readonly Guid VolunteerClubId = Guid.Parse("20000000-0000-0000-0000-000000000004");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var defaultPassword = BCrypt.Net.BCrypt.HashPassword("ClubHub@123");

        var admin = new User
        {
            Id = AdminId,
            FullName = "University Admin",
            Username = "university.admin",
            Email = "admin@clubhub.local",
            PasswordHash = defaultPassword,
            SystemRole = SystemRole.UniversityAdmin,
            CreatedAt = now
        };

        var studentOne = new User
        {
            Id = StudentOneId,
            FullName = "Nguyen Minh Anh",
            Username = "minhanh",
            Email = "minhanh@student.clubhub.local",
            PasswordHash = defaultPassword,
            StudentCode = "SE170001",
            Phone = "0900000001",
            CreatedAt = now
        };

        var studentTwo = new User
        {
            Id = StudentTwoId,
            FullName = "Tran Bao Long",
            Username = "baolong",
            Email = "baolong@student.clubhub.local",
            PasswordHash = defaultPassword,
            StudentCode = "SE170002",
            Phone = "0900000002",
            CreatedAt = now
        };

        var studentThree = new User
        {
            Id = StudentThreeId,
            FullName = "Le Gia Han",
            Username = "giahan",
            Email = "giahan@student.clubhub.local",
            PasswordHash = defaultPassword,
            StudentCode = "SE170003",
            Phone = "0900000003",
            CreatedAt = now
        };

        var clubs = new List<Club>
        {
            new()
            {
                Id = TechClubId,
                Name = "CodeCraft Technology Club",
                Category = ClubCategory.Technology,
                Description = "Student builders learning software engineering through products, contests, and workshops.",
                LogoUrl = "https://example.com/logos/codecraft.png",
                CoverImageUrl = "https://example.com/covers/codecraft.jpg",
                CreatedBy = StudentOneId,
                CreatedAt = now
            },
            new()
            {
                Id = AcademicClubId,
                Name = "Research Circle",
                Category = ClubCategory.Academic,
                Description = "Academic mentoring, research reading groups, and peer study sessions.",
                LogoUrl = "https://example.com/logos/research-circle.png",
                CreatedBy = StudentTwoId,
                CreatedAt = now
            },
            new()
            {
                Id = SportsClubId,
                Name = "Campus Runners",
                Category = ClubCategory.Sports,
                Description = "Weekly running sessions and university sports event preparation.",
                LogoUrl = "https://example.com/logos/campus-runners.png",
                CreatedBy = StudentThreeId,
                CreatedAt = now
            },
            new()
            {
                Id = VolunteerClubId,
                Name = "Green Hearts Volunteer Club",
                Category = ClubCategory.Volunteer,
                Description = "Community service, charity events, and environmental campaigns.",
                LogoUrl = "https://example.com/logos/green-hearts.png",
                CreatedBy = StudentOneId,
                CreatedAt = now
            }
        };

        var techWorkshopId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var techDemoDayId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var techDraftPlanningId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        db.Users.AddRange(admin, studentOne, studentTwo, studentThree);
        db.Clubs.AddRange(clubs);
        db.ClubMembers.AddRange(
            new ClubMember
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                UserId = StudentOneId,
                ClubId = TechClubId,
                RoleInClub = ClubRole.President,
                Status = MembershipStatus.Approved,
                RequestedAt = now.AddDays(-20),
                JoinedAt = now.AddDays(-20)
            },
            new ClubMember
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                UserId = StudentTwoId,
                ClubId = TechClubId,
                RoleInClub = ClubRole.VicePresident,
                Status = MembershipStatus.Approved,
                RequestedAt = now.AddDays(-15),
                JoinedAt = now.AddDays(-15)
            },
            new ClubMember
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                UserId = StudentThreeId,
                ClubId = TechClubId,
                RoleInClub = ClubRole.Member,
                Status = MembershipStatus.Pending,
                JoinReason = "I want to improve backend engineering skills.",
                RequestedAt = now.AddDays(-1)
            },
            new ClubMember
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                UserId = StudentTwoId,
                ClubId = AcademicClubId,
                RoleInClub = ClubRole.President,
                Status = MembershipStatus.Approved,
                RequestedAt = now.AddDays(-18),
                JoinedAt = now.AddDays(-18)
            });

        db.Events.AddRange(
            new Event
            {
                Id = techWorkshopId,
                ClubId = TechClubId,
                Name = "Backend API Workshop",
                Description = "Hands-on REST API design and JWT authentication workshop.",
                Location = "Lab A204",
                StartTime = now.AddDays(7),
                EndTime = now.AddDays(7).AddHours(2),
                Capacity = 40,
                Status = EventStatus.Published,
                CreatedBy = StudentOneId,
                CreatedAt = now.AddDays(-3)
            },
            new Event
            {
                Id = techDemoDayId,
                ClubId = TechClubId,
                Name = "Mini Product Demo Day",
                Description = "Members present small projects and collect peer feedback.",
                Location = "Hall B",
                StartTime = now.AddDays(-5),
                EndTime = now.AddDays(-5).AddHours(3),
                Capacity = 50,
                Status = EventStatus.Completed,
                CreatedBy = StudentTwoId,
                CreatedAt = now.AddDays(-12)
            },
            new Event
            {
                Id = techDraftPlanningId,
                ClubId = TechClubId,
                Name = "Internal Leadership Planning",
                Description = "Draft-only planning session for club managers.",
                Location = "Online",
                StartTime = now.AddDays(14),
                EndTime = now.AddDays(14).AddHours(1),
                Capacity = 10,
                Status = EventStatus.Draft,
                CreatedBy = StudentOneId,
                CreatedAt = now.AddDays(-1)
            });

        db.EventRegistrations.Add(new EventRegistration
        {
            Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            EventId = techDemoDayId,
            UserId = StudentTwoId,
            IsCheckedIn = true,
            CheckInTime = now.AddDays(-5).AddMinutes(5),
            RegisteredAt = now.AddDays(-10)
        });

        db.Feedbacks.Add(new Feedback
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            EventId = techDemoDayId,
            UserId = StudentTwoId,
            Rating = 5,
            Comment = "Useful format and good peer review.",
            CreatedAt = now.AddDays(-4)
        });

        db.PointTransactions.AddRange(
            new PointTransaction
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                UserId = StudentTwoId,
                ClubId = TechClubId,
                Points = 10,
                Type = PointType.CheckIn,
                Note = "Check in event: Mini Product Demo Day",
                ReferenceId = techDemoDayId,
                CreatedAt = now.AddDays(-5)
            },
            new PointTransaction
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                UserId = StudentTwoId,
                ClubId = TechClubId,
                Points = 2,
                Type = PointType.Feedback,
                Note = "Submitted feedback after event",
                ReferenceId = techDemoDayId,
                CreatedAt = now.AddDays(-4)
            },
            new PointTransaction
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                UserId = StudentOneId,
                ClubId = TechClubId,
                Points = 15,
                Type = PointType.Support,
                Note = "Supported event organization",
                ReferenceId = techDemoDayId,
                CreatedAt = now.AddDays(-5)
            });

        db.ClubProposals.AddRange(
            new ClubProposal
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000001"),
                ClubName = "Startup Lab",
                Category = ClubCategory.Startup,
                Description = "A club for validating student startup ideas.",
                Mission = "Help students turn ideas into prototypes.",
                Reason = "Students need a structured startup community.",
                ActivityPlan = "Monthly ideation sessions and demo days.",
                FounderInfo = "Le Gia Han",
                FounderStudentCode = "SE170003",
                ContactEmail = "giahan@student.clubhub.local",
                ContactPhone = "0900000003",
                ProposalFileUrl = "https://example.com/proposals/startup-lab.pdf",
                Notes = "Looking for an advisor from business faculty.",
                SubmittedBy = StudentThreeId,
                Status = ProposalStatus.Pending,
                SubmittedAt = now.AddDays(-2)
            },
            new ClubProposal
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000002"),
                ClubName = "Media Studio",
                Category = ClubCategory.Media,
                Description = "Campus video and podcast production group.",
                FounderInfo = "Tran Bao Long",
                FounderStudentCode = "SE170002",
                ContactEmail = "baolong@student.clubhub.local",
                ProposalFileUrl = "https://example.com/proposals/media-studio.pdf",
                SubmittedBy = StudentTwoId,
                Status = ProposalStatus.NeedMoreInfo,
                RejectionReason = "Please add a semester activity plan and advisor name.",
                SubmittedAt = now.AddDays(-8),
                ReviewedBy = AdminId,
                ReviewedAt = now.AddDays(-6)
            });

        db.Announcements.Add(new Announcement
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
            ClubId = TechClubId,
            Title = "Workshop preparation",
            Content = "Bring a laptop and install .NET SDK before the backend workshop.",
            IsPinned = true,
            CreatedBy = StudentOneId,
            CreatedAt = now.AddDays(-1)
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            ClubId = TechClubId,
            ActorUserId = StudentOneId,
            Action = "SeedDataCreated",
            TargetType = nameof(Club),
            TargetId = TechClubId,
            Description = "Initial sample activity history.",
            CreatedAt = now
        });

        db.Notifications.AddRange(
            new Notification
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                UserId = StudentThreeId,
                Title = "Join request submitted",
                Content = "Your request to join CodeCraft Technology Club is waiting for review.",
                Type = "JOIN_REQUEST_PENDING",
                IsRead = false,
                CreatedAt = now.AddHours(-6)
            },
            new Notification
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
                UserId = StudentTwoId,
                Title = "Event reminder",
                Content = "Backend API Workshop is coming up soon.",
                Type = "EVENT_REMINDER",
                IsRead = false,
                CreatedAt = now.AddHours(-2)
            },
            new Notification
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000003"),
                UserId = StudentOneId,
                Title = "Announcement published",
                Content = "Your pinned announcement is visible to club members.",
                Type = "ANNOUNCEMENT",
                IsRead = true,
                CreatedAt = now.AddDays(-1)
            });

        await db.SaveChangesAsync();
    }
}
