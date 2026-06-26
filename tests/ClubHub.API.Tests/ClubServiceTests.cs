using ClubHub.API.DTOs.Club;
using ClubHub.API.Enums;
using Xunit;

namespace ClubHub.API.Tests;

public class ClubServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Club_List_Supports_Search_And_Category_Filter()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Clubs(db);

        var tech = await service.GetAllAsync(new ClubFilterRequest
        {
            Category = ClubCategory.Technology,
            SearchTerm = "Code",
            Page = 1,
            PageSize = 10
        });

        Assert.Single(tech.Items);
        Assert.Equal("CodeCraft Technology Club", tech.Items[0].Name);
    }
}
