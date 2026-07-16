using ClubHub.API.Data;
using Xunit;

namespace ClubHub.API.Tests;

public class StatisticsServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Club_Statistics_Require_Manager_Or_University_Admin()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Statistics(db);

        var president = await service.GetClubStatisticsForUserAsync(DataSeeder.TechClubId, DataSeeder.StudentOneId);
        var pendingMember = await service.GetClubStatisticsForUserAsync(DataSeeder.TechClubId, DataSeeder.StudentThreeId);
        var admin = await service.GetClubStatisticsForUserAsync(DataSeeder.TechClubId, DataSeeder.AdminId);

        Assert.True(president.IsSuccess);
        Assert.True(admin.IsSuccess);
        Assert.False(pendingMember.IsSuccess);
        Assert.True(president.Data!.MemberCount >= 2);
        Assert.True(president.Data.EventCount >= 1);
    }
}
