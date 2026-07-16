using ClubHub.API.Data;
using ClubHub.API.DTOs.Event;
using Xunit;

namespace ClubHub.API.Tests;

public class EventServiceTests : ServiceTestBase
{
    private static readonly Guid PublicEventId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid DraftEventId = Guid.Parse("30000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task Public_Event_List_Excludes_Draft_And_Upcoming_Returns_Future_Published_Events()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Events(db);

        var allPublic = await service.GetPublicEventsAsync(new EventFilterRequest { Page = 1, PageSize = 20 }, upcomingOnly: false);
        var upcoming = await service.GetPublicEventsAsync(new EventFilterRequest { Page = 1, PageSize = 20 }, upcomingOnly: true);

        Assert.DoesNotContain(allPublic.Items, e => e.Id == DraftEventId);
        Assert.Contains(allPublic.Items, e => e.Id == PublicEventId);
        Assert.Contains(upcoming.Items, e => e.Id == PublicEventId);
        Assert.All(upcoming.Items, e => Assert.True(e.StartTime >= DateTime.UtcNow));
    }

    [Fact]
    public async Task Event_Detail_Authorization_Protects_Draft_Events()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Events(db);

        var publicForGuest = await service.GetEventByIdAsync(PublicEventId);
        var draftForGuest = await service.GetEventByIdAsync(DraftEventId);
        var draftForPendingNonMember = await service.GetEventByIdAsync(DraftEventId, DataSeeder.StudentThreeId);
        var draftForClubOfficer = await service.GetEventByIdAsync(DraftEventId, DataSeeder.StudentTwoId);
        var draftForUniversityAdmin = await service.GetEventByIdAsync(DraftEventId, DataSeeder.AdminId);

        Assert.NotNull(publicForGuest);
        Assert.Null(draftForGuest);
        Assert.Null(draftForPendingNonMember);
        Assert.NotNull(draftForClubOfficer);
        Assert.NotNull(draftForUniversityAdmin);
    }
}
