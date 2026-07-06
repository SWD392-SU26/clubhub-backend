using ClubHub.API.Entities;

namespace ClubHub.API.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
}
