using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClubHub.API.Data;
using ClubHub.API.DTOs.Auth;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClubHub.API.Services.Interfaces;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        IConfiguration config,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResult<LoginResponse>> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return ApiResult<LoginResponse>.Failure("Email đã tồn tại.");

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return ApiResult<LoginResponse>.Failure("Username đã tồn tại.");

        var user = new User
        {
            FullName = req.FullName,
            Username = req.Username,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            StudentCode = req.StudentCode,
            Phone = req.Phone
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await GenerateLoginResponseAsync(user);
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == req.EmailOrUsername || u.Username == req.EmailOrUsername);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ApiResult<LoginResponse>.Failure("Thông tin đăng nhập không hợp lệ.");

        if (!user.IsActive)
            return ApiResult<LoginResponse>.Failure("Tài khoản đã bị vô hiệu hóa.");

        return await GenerateLoginResponseAsync(user);
    }

    public async Task<ApiResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return ApiResult<LoginResponse>.Failure("Refresh token không hợp lệ hoặc đã hết hạn.");

        return await GenerateLoginResponseAsync(user);
    }

    public async Task<ApiResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest req)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResult<bool>.Failure("Người dùng không tồn tại.");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return ApiResult<bool>.Failure("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return ApiResult<bool>.Success(true); // Don't reveal whether email exists

        var resetToken = GenerateSecureToken();
        user.PasswordResetToken = HashToken(resetToken);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetToken);
        }
        catch (Exception ex)
        {
            // Keep the public response indistinguishable from an unknown email response.
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.Id);
        }

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> ResetPasswordAsync(ResetPasswordRequest req)
    {
        var tokenHash = HashToken(req.Token);
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == tokenHash && u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return ApiResult<bool>.Failure("Token không hợp lệ hoặc đã hết hạn.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<UserProfileDto>> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResult<UserProfileDto>.Failure("Người dùng không tồn tại.");
        return ApiResult<UserProfileDto>.Success(MapToProfile(user));
    }

    public async Task<ApiResult<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResult<UserProfileDto>.Failure("Người dùng không tồn tại.");

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.Phone != null) user.Phone = req.Phone;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResult<UserProfileDto>.Success(MapToProfile(user));
    }

    public async Task<ApiResult<bool>> LogoutAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return ApiResult<bool>.Failure("Người dùng không tồn tại.");
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ApiResult<LoginResponse>> GenerateLoginResponseAsync(User user)
    {
        var accessToken = GenerateJwt(user);
        var refreshToken = GenerateSecureToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return ApiResult<LoginResponse>.Success(new LoginResponse(accessToken, refreshToken, MapToProfile(user)));
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.SystemRole.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static UserProfileDto MapToProfile(User u) => new(
        u.Id, u.FullName, u.Username, u.Email,
        u.StudentCode, u.Phone, u.AvatarUrl,
        u.SystemRole.ToString(), u.CreatedAt
    );
}
