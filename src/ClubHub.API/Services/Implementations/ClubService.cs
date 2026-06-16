using ClubHub.API.Data;
using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class ClubService : IClubService
{
    private readonly AppDbContext _db;

    public ClubService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ClubSummaryDto>> GetAllAsync(ClubFilterRequest filter)
    {
        var query = _db.Clubs
            .Where(c => c.Status == ClubStatus.Active)
            .AsQueryable();

        if (filter.Category.HasValue)
            query = query.Where(c => c.Category == filter.Category.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(c => c.Name.Contains(filter.SearchTerm) ||
                                     (c.Description != null && c.Description.Contains(filter.SearchTerm)));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new ClubSummaryDto(
                c.Id, c.Name, c.Category.ToString(), c.Description,
                c.LogoUrl, c.CoverImageUrl, c.Status.ToString(),
                c.Members.Count(m => m.Status == MembershipStatus.Approved),
                c.CreatedAt))
            .ToListAsync();

        return new PagedResult<ClubSummaryDto>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ClubDetailDto?> GetByIdAsync(Guid clubId, Guid? currentUserId = null)
    {
        var club = await _db.Clubs
            .Include(c => c.Members.Where(m => m.Status == MembershipStatus.Approved))
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == clubId && c.Status != ClubStatus.Deleted);

        if (club == null) return null;

        var officers = club.Members
            .Where(m => m.RoleInClub != ClubRole.Member)
            .Select(m => new ClubOfficerDto(m.UserId, m.User.FullName, m.User.AvatarUrl, m.RoleInClub.ToString()))
            .ToList();

        return new ClubDetailDto(
            club.Id, club.Name, club.Category.ToString(), club.Description,
            club.LogoUrl, club.CoverImageUrl, club.Status.ToString(),
            club.Members.Count, officers, club.CreatedAt);
    }

    public async Task<ApiResult<ClubDetailDto>> CreateClubAsync(CreateClubRequest req, Guid createdBy)
    {
        var club = new Club
        {
            Name = req.Name,
            Category = req.Category,
            Description = req.Description,
            LogoUrl = req.LogoUrl,
            CoverImageUrl = req.CoverImageUrl,
            CreatedBy = createdBy
        };

        _db.Clubs.Add(club);

        // Auto-add creator as President
        _db.ClubMembers.Add(new ClubMember
        {
            UserId = createdBy,
            ClubId = club.Id,
            RoleInClub = ClubRole.President,
            Status = MembershipStatus.Approved,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        var detail = await GetByIdAsync(club.Id);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public async Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest req, Guid requesterId)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<ClubDetailDto>.Failure("CLB không tồn tại.");

        var isAdmin = await IsClubAdminAsync(clubId, requesterId);
        if (!isAdmin) return ApiResult<ClubDetailDto>.Failure("Bạn không có quyền cập nhật CLB này.");

        if (req.Name != null) club.Name = req.Name;
        if (req.Description != null) club.Description = req.Description;
        if (req.LogoUrl != null) club.LogoUrl = req.LogoUrl;
        if (req.CoverImageUrl != null) club.CoverImageUrl = req.CoverImageUrl;
        club.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        var detail = await GetByIdAsync(clubId);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public async Task<ApiResult<bool>> HideClubAsync(Guid clubId)
        => await ChangeStatusAsync(clubId, ClubStatus.Hidden);

    public async Task<ApiResult<bool>> LockClubAsync(Guid clubId)
        => await ChangeStatusAsync(clubId, ClubStatus.Locked);

    public async Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, bool hardDelete = false)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("CLB không tồn tại.");

        if (hardDelete)
            _db.Clubs.Remove(club);
        else
        {
            club.Status = ClubStatus.Deleted;
            club.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize)
    {
        var query = _db.ClubMembers
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Approved)
            .Select(m => m.Club)
            .Where(c => c.Status != ClubStatus.Deleted);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClubSummaryDto(
                c.Id, c.Name, c.Category.ToString(), c.Description,
                c.LogoUrl, c.CoverImageUrl, c.Status.ToString(),
                c.Members.Count(m => m.Status == MembershipStatus.Approved),
                c.CreatedAt))
            .ToListAsync();

        return new PagedResult<ClubSummaryDto>(items, page, pageSize, total);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            (m.RoleInClub == ClubRole.ClubAdmin || m.RoleInClub == ClubRole.President));

    private async Task<ApiResult<bool>> ChangeStatusAsync(Guid clubId, ClubStatus status)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("CLB không tồn tại.");
        club.Status = status;
        club.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }
}
