using ClubHub.API.DTOs.Auth;
using ClubHub.API.Services.Interfaces;
using Xunit;

namespace ClubHub.API.Tests;

public class AuthServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Register_Then_Login_Returns_Jwt_And_Profile()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new AuthService(db, CreateConfiguration());

        var register = await service.RegisterAsync(new RegisterRequest(
            "New Student",
            "newstudent",
            "newstudent@student.clubhub.local",
            "Password@123",
            "SE170099",
            "0900000099"));

        Assert.True(register.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(register.Data!.AccessToken));

        var login = await service.LoginAsync(new LoginRequest("newstudent", "Password@123"));

        Assert.True(login.IsSuccess);
        Assert.Equal("newstudent", login.Data!.Profile.Username);
        Assert.False(string.IsNullOrWhiteSpace(login.Data.AccessToken));
    }
}
