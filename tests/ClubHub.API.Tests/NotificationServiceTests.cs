using ClubHub.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClubHub.API.Tests;

public class NotificationServiceTests : ServiceTestBase
{
    [Fact]
    public async Task User_Can_Read_Own_Unread_Count_And_Mark_Notification_As_Read()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Notifications(db);

        var before = await service.GetUnreadCountAsync(DataSeeder.StudentTwoId);
        Assert.Equal(1, before.Count);

        var notificationId = await db.Notifications
            .Where(n => n.UserId == DataSeeder.StudentTwoId && !n.IsRead)
            .Select(n => n.Id)
            .SingleAsync();

        var mark = await service.MarkAsReadAsync(notificationId, DataSeeder.StudentTwoId);
        var after = await service.GetUnreadCountAsync(DataSeeder.StudentTwoId);

        Assert.True(mark.IsSuccess);
        Assert.Equal(0, after.Count);
    }

    [Fact]
    public async Task User_Cannot_Mark_Another_Users_Notification()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Notifications(db);

        var otherNotificationId = await db.Notifications
            .Where(n => n.UserId == DataSeeder.StudentThreeId)
            .Select(n => n.Id)
            .SingleAsync();

        var result = await service.MarkAsReadAsync(otherNotificationId, DataSeeder.StudentTwoId);

        Assert.False(result.IsSuccess);
    }
}
