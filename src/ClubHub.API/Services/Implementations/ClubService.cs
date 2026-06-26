using ClubHub.API.Data;
using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class ClubService : IClubService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public ClubService(AppDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    public Task<PagedResult<ClubSummaryDto>> GetAllAsync(ClubFilterRequest filter)
        => GetClubsAsync(filter, includeAllStatuses: false);

    public Task<PagedResult<ClubSummaryDto>> GetAdminClubsAsync(ClubFilterRequest filter)
        => GetClubsAsync(filter, includeAllStatuses: true);

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
        _db.ClubMembers.Add(new ClubMember
        {
            UserId = createdBy,
            ClubId = club.Id,
            RoleInClub = ClubRole.President,
            Status = MembershipStatus.Approved,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(club.Id, createdBy, "ClubCreated", nameof(Club), club.Id, createdBy, $"Created club '{club.Name}'.");

        var detail = await GetByIdAsync(club.Id);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public async Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest req, Guid requesterId)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<ClubDetailDto>.Failure("Club does not exist.");

        if (!await IsClubAdminAsync(clubId, requesterId))
            return ApiResult<ClubDetailDto>.Failure("You do not have permission to update this club.");

        if (req.Name != null) club.Name = req.Name;
        if (req.Description != null) club.Description = req.Description;
        if (req.LogoUrl != null) club.LogoUrl = req.LogoUrl;
        if (req.CoverImageUrl != null) club.CoverImageUrl = req.CoverImageUrl;
        club.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, "ClubUpdated", nameof(Club), clubId, description: $"Updated club '{club.Name}'.");

        var detail = await GetByIdAsync(clubId);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public Task<ApiResult<bool>> HideClubAsync(Guid clubId, Guid requesterId)
        => ChangeStatusAsync(clubId, ClubStatus.Hidden, requesterId, "ClubHidden");

    public Task<ApiResult<bool>> LockClubAsync(Guid clubId, Guid requesterId)
        => ChangeStatusAsync(clubId, ClubStatus.Locked, requesterId, "ClubLocked");

    public async Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, Guid requesterId, bool hardDelete = false)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("Club does not exist.");

        if (hardDelete)
        {
            _db.Clubs.Remove(club);
            await _db.SaveChangesAsync();
            return ApiResult<bool>.Success(true);
        }

        else
        {
            club.Status = ClubStatus.Deleted;
            club.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, "ClubSoftDeleted", nameof(Club), clubId, description: $"Deleted club '{club.Name}'.");
        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ClubMembers
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Approved)
            .Select(m => m.Club)
            .Where(c => c.Status != ClubStatus.Deleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToSummary(c))
            .ToListAsync();

        return new PagedResult<ClubSummaryDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<ClubSummaryDto>> GetManagedClubsAsync(Guid userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ClubMembers
            .Where(m =>
                m.UserId == userId &&
                m.Status == MembershipStatus.Approved &&
                (m.RoleInClub == ClubRole.ClubAdmin ||
                 m.RoleInClub == ClubRole.President ||
                 m.RoleInClub == ClubRole.VicePresident))
            .Select(m => m.Club)
            .Where(c => c.Status != ClubStatus.Deleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToSummary(c))
            .ToListAsync();

        return new PagedResult<ClubSummaryDto>(items, page, pageSize, total);
    }

    private async Task<PagedResult<ClubSummaryDto>> GetClubsAsync(ClubFilterRequest filter, bool includeAllStatuses)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _db.Clubs.AsQueryable();
        if (!includeAllStatuses)
            query = query.Where(c => c.Status == ClubStatus.Active);

        if (filter.Category.HasValue)
            query = query.Where(c => c.Category == filter.Category.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(c => c.Name.Contains(filter.SearchTerm) ||
                                     (c.Description != null && c.Description.Contains(filter.SearchTerm)));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToSummary(c))
            .ToListAsync();

        return new PagedResult<ClubSummaryDto>(items, page, pageSize, total);
    }

    private async Task<ApiResult<bool>> ChangeStatusAsync(Guid clubId, ClubStatus status, Guid requesterId, string action)
    {
        var club = await _db.Clubs.FindAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("Club does not exist.");

        club.Status = status;
        club.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(clubId, requesterId, action, nameof(Club), clubId, description: $"Changed club '{club.Name}' status to {status}.");

        return ApiResult<bool>.Success(true);
    }

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private static ClubSummaryDto MapToSummary(Club c) => new(
        c.Id, c.Name, c.Category.ToString(), c.Description,
        c.LogoUrl, c.CoverImageUrl, c.Status.ToString(),
        c.Members.Count(m => m.Status == MembershipStatus.Approved),
        c.CreatedAt);
}
