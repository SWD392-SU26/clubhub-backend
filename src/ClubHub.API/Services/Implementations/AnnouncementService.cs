using ClubHub.API.Data;
using ClubHub.API.DTOs.Announcement;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;

    public AnnouncementService(AppDbContext db, IAuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<AnnouncementDto>> GetClubAnnouncementsAsync(Guid clubId, Guid requesterId, int page, int pageSize)
    {
        if (!await CanReadClubAsync(clubId, requesterId))
            throw new UnauthorizedAccessException("You must be a club member to view internal announcements.");

        var query = _db.Announcements
            .Include(a => a.Creator)
            .Where(a => a.ClubId == clubId && !a.IsArchived)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new PagedResult<AnnouncementDto>(items, page, pageSize, total);
    }

    public async Task<ApiResult<AnnouncementDto>> CreateAsync(Guid clubId, CreateAnnouncementRequest request, Guid requesterId)
    {
        if (!await IsClubManagerAsync(clubId, requesterId))
            return ApiResult<AnnouncementDto>.Failure("You do not have permission to create club announcements.");

        var announcement = new Announcement
        {
            ClubId = clubId,
            Title = request.Title,
            Content = request.Content,
            IsPinned = request.IsPinned,
            CreatedBy = requesterId
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync(
            clubId,
            requesterId,
            "AnnouncementCreated",
            nameof(Announcement),
            announcement.Id,
            description: $"Created announcement '{announcement.Title}'.");

        var created = await _db.Announcements
            .Include(a => a.Creator)
            .FirstAsync(a => a.Id == announcement.Id);

        return ApiResult<AnnouncementDto>.Success(MapToDto(created));
    }

    public async Task<ApiResult<AnnouncementDto>> UpdateAsync(Guid clubId, Guid announcementId, UpdateAnnouncementRequest request, Guid requesterId)
    {
        if (!await IsClubManagerAsync(clubId, requesterId))
            return ApiResult<AnnouncementDto>.Failure("You do not have permission to update club announcements.");

        var announcement = await _db.Announcements
            .Include(a => a.Creator)
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.ClubId == clubId && !a.IsArchived);

        if (announcement == null)
            return ApiResult<AnnouncementDto>.Failure("Announcement does not exist.");

        if (request.Title != null) announcement.Title = request.Title;
        if (request.Content != null) announcement.Content = request.Content;
        if (request.IsPinned.HasValue) announcement.IsPinned = request.IsPinned.Value;
        announcement.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _auditLogService.LogAsync(
            clubId,
            requesterId,
            "AnnouncementUpdated",
            nameof(Announcement),
            announcement.Id,
            description: $"Updated announcement '{announcement.Title}'.");

        return ApiResult<AnnouncementDto>.Success(MapToDto(announcement));
    }

    public async Task<ApiResult<bool>> ArchiveAsync(Guid clubId, Guid announcementId, Guid requesterId)
    {
        if (!await IsClubManagerAsync(clubId, requesterId))
            return ApiResult<bool>.Failure("You do not have permission to archive club announcements.");

        var announcement = await _db.Announcements
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.ClubId == clubId && !a.IsArchived);

        if (announcement == null)
            return ApiResult<bool>.Failure("Announcement does not exist.");

        announcement.IsArchived = true;
        announcement.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLogService.LogAsync(
            clubId,
            requesterId,
            "AnnouncementArchived",
            nameof(Announcement),
            announcement.Id,
            description: $"Archived announcement '{announcement.Title}'.");

        return ApiResult<bool>.Success(true);
    }

    private async Task<bool> CanReadClubAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId && m.Status == MembershipStatus.Approved);

    private async Task<bool> IsClubManagerAsync(Guid clubId, Guid userId)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.SystemRole == SystemRole.UniversityAdmin)
           || await _db.ClubMembers.AnyAsync(m =>
               m.ClubId == clubId && m.UserId == userId &&
               m.Status == MembershipStatus.Approved &&
               (m.RoleInClub == ClubRole.ClubAdmin ||
                m.RoleInClub == ClubRole.President ||
                m.RoleInClub == ClubRole.VicePresident));

    private static AnnouncementDto MapToDto(Announcement a) => new(
        a.Id,
        a.ClubId,
        a.Title,
        a.Content,
        a.IsPinned,
        a.CreatedBy,
        a.Creator.FullName,
        a.CreatedAt,
        a.UpdatedAt);
}
