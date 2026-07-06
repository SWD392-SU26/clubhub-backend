using ClubHub.API.Data;
using ClubHub.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
        => await _dbSet.FirstOrDefaultAsync(u =>
            u.Email == emailOrUsername || u.Username == emailOrUsername);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        => await _dbSet.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

    public async Task<User?> GetByPasswordResetTokenAsync(string token)
        => await _dbSet.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _dbSet.AnyAsync(u => u.Email == email);

    public async Task<bool> ExistsByUsernameAsync(string username)
        => await _dbSet.AnyAsync(u => u.Username == username);
}
