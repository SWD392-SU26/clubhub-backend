using ClubHub.API.Data;
using ClubHub.API.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClubHub.API.Tests;

public abstract class ServiceTestBase
{
    protected static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        await DataSeeder.SeedAsync(db);
        return db;
    }

    protected static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ClubHub_Test_Secret_Key_With_Enough_Length_12345!",
                ["Jwt:Issuer"] = "ClubHub.Tests",
                ["Jwt:Audience"] = "ClubHub.Tests",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
    }

    protected static AuditLogService Audit(AppDbContext db) => new(db);
    protected static ClubService Clubs(AppDbContext db) => new(db, Audit(db));
    protected static PointService Points(AppDbContext db) => new(db, Audit(db));
    protected static MembershipService Memberships(AppDbContext db) => new(db, Audit(db));
    protected static ProposalService Proposals(AppDbContext db) => new(db, Clubs(db));
    protected static NotificationService Notifications(AppDbContext db) => new(db);
    protected static EventService Events(AppDbContext db) => new(db, Points(db), Audit(db));
    protected static AdminStatisticsService Statistics(AppDbContext db) => new(db);
}
