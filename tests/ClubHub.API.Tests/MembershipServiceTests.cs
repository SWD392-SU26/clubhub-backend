using ClubHub.API.Data;
using ClubHub.API.DTOs.Membership;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClubHub.API.Tests;

public class MembershipServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Join_Request_Can_Be_Approved_By_Club_President()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Memberships(db);

        var request = await service.RequestJoinAsync(
            DataSeeder.AcademicClubId,
            DataSeeder.StudentThreeId,
            new JoinClubRequest("I want to join research mentoring."));

        Assert.True(request.IsSuccess);

        var pending = await db.ClubMembers.SingleAsync(m =>
            m.ClubId == DataSeeder.AcademicClubId &&
            m.UserId == DataSeeder.StudentThreeId);

        var review = await service.ReviewRequestAsync(
            pending.Id,
            DataSeeder.StudentTwoId,
            new ReviewMembershipRequest(true, null));

        Assert.True(review.IsSuccess);
        Assert.Equal(MembershipStatus.Approved, pending.Status);
        Assert.Equal(ClubRole.Member, pending.RoleInClub);
        Assert.NotNull(pending.JoinedAt);
    }
}
