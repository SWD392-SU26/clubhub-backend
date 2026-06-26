using ClubHub.API.Data;
using ClubHub.API.DTOs.Point;
using ClubHub.API.Enums;
using Xunit;

namespace ClubHub.API.Tests;

public class PointServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Manual_Point_Transaction_Updates_Ranking()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Points(db);

        var transaction = await service.AddPointTransactionAsync(
            DataSeeder.TechClubId,
            new CreatePointTransactionRequest(
                DataSeeder.StudentTwoId,
                5,
                PointType.Activity,
                "Joined internal activity",
                null),
            DataSeeder.StudentOneId);

        Assert.True(transaction.IsSuccess);

        var ranking = await service.GetClubLeaderboardAsync(DataSeeder.TechClubId, 1, 10);

        Assert.Equal(DataSeeder.StudentTwoId, ranking.Items[0].UserId);
        Assert.Equal(17, ranking.Items[0].TotalPoints);
    }
}
