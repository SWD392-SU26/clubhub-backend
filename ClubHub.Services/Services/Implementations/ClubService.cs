using ClubHub.API.DTOs.Auth;
using ClubHub.API.DTOs.Club;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class ClubService : IClubService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public ClubService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<PagedResult<ClubSummaryDto>> GetAllAsync(ClubFilterRequest filter)
    {
        var query = _uow.Clubs.QueryActiveClubs();

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

    public async Task<PagedResult<ClubSummaryDto>> GetAllByStatusAsync(ClubStatus? status, int page, int pageSize)
    {
        var query = _uow.Clubs.QueryAllClubs();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
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

    public async Task<ClubDetailDto?> GetByIdAsync(Guid clubId, Guid? currentUserId = null)
    {
        var club = await _uow.Clubs.GetClubWithMembersAsync(clubId);

        if (club == null) return null;

        var officers = club.Members
            .Where(m => m.RoleInClub == Role.ClubAdmin)
            .Select(m => new ClubOfficerDto(m.UserId, m.User.FullName, m.User.AvatarUrl, m.RoleInClub.ToString()))
            .ToList();

        return new ClubDetailDto(
            club.Id, club.Name, club.Category.ToString(), club.Description,
            club.LogoUrl, club.CoverImageUrl, club.Status.ToString(),
            club.Members.Count(m => m.Status == MembershipStatus.Approved), officers, club.CreatedAt);
    }

    /// <summary>Tạo CLB, thêm người tạo làm ClubAdmin (dùng nội bộ và từ Proposal)</summary>
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

        _uow.Clubs.Add(club);

        // Auto-add creator as ClubAdmin in this club
        _uow.ClubMembers.Add(new ClubMember
        {
            UserId = createdBy,
            ClubId = club.Id,
            RoleInClub = Role.ClubAdmin,
            Status = MembershipStatus.Approved,
            JoinedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();
        var detail = await GetByIdAsync(club.Id);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    /// <summary>UniversityAdmin tạo CLB và chỉ định ClubAdmin (Role.ClubAdmin) từ select list</summary>
    public async Task<ApiResult<ClubDetailDto>> CreateClubWithAdminAsync(CreateClubWithAdminRequest req, Guid createdByUniAdmin)
    {
        // Validate that selected user exists and is a ClubAdmin
        var adminUser = await _uow.Users.GetByIdAsync(req.ClubAdminUserId);
        if (adminUser == null)
            return ApiResult<ClubDetailDto>.Failure("Người dùng được chỉ định không tồn tại.");
        if (adminUser.Role != Role.ClubAdmin)
            return ApiResult<ClubDetailDto>.Failure("Người dùng được chỉ định phải có vai trò ClubAdmin.");

        var club = new Club
        {
            Name = req.Name,
            Category = req.Category,
            Description = req.Description,
            LogoUrl = req.LogoUrl,
            CoverImageUrl = req.CoverImageUrl,
            CreatedBy = createdByUniAdmin
        };

        _uow.Clubs.Add(club);

        _uow.ClubMembers.Add(new ClubMember
        {
            UserId = req.ClubAdminUserId,
            ClubId = club.Id,
            RoleInClub = Role.ClubAdmin,
            Status = MembershipStatus.Approved,
            JoinedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Club", club.Id, "CreateWithAdmin", createdByUniAdmin,
            null, club.Id, null, $"Tạo CLB {club.Name} với ClubAdmin: {adminUser.FullName}");

        var detail = await GetByIdAsync(club.Id);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public async Task<ApiResult<ClubDetailDto>> UpdateClubAsync(Guid clubId, UpdateClubRequest req, Guid requesterId)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null) return ApiResult<ClubDetailDto>.Failure("CLB không tồn tại.");

        var isAdmin = await IsClubAdminAsync(clubId, requesterId);
        if (!isAdmin) return ApiResult<ClubDetailDto>.Failure("Bạn không có quyền cập nhật CLB này.");

        if (req.Name != null) club.Name = req.Name;
        if (req.Description != null) club.Description = req.Description;
        if (req.LogoUrl != null) club.LogoUrl = req.LogoUrl;
        if (req.CoverImageUrl != null) club.CoverImageUrl = req.CoverImageUrl;
        club.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        var detail = await GetByIdAsync(clubId);
        return ApiResult<ClubDetailDto>.Success(detail!);
    }

    public async Task<ApiResult<bool>> UpdateStatusAsync(Guid clubId, ClubStatus status)
    {
        return await ChangeStatusAsync(clubId, status);
    }

    public async Task<ApiResult<bool>> HideClubAsync(Guid clubId)
    {
        var result = await ChangeStatusAsync(clubId, ClubStatus.Inactive);
        if (result.IsSuccess)
            await _auditService.LogAsync("Club", clubId, "Hide", null, null, clubId, null, "CLB bị ẩn (Inactive)");
        return result;
    }

    public async Task<ApiResult<bool>> LockClubAsync(Guid clubId)
    {
        var result = await ChangeStatusAsync(clubId, ClubStatus.Lock);
        if (result.IsSuccess)
            await _auditService.LogAsync("Club", clubId, "Lock", null, null, clubId, null, "CLB bị khóa");
        return result;
    }

    public async Task<ApiResult<bool>> ArchiveClubAsync(Guid clubId)
    {
        var result = await ChangeStatusAsync(clubId, ClubStatus.Inactive);
        if (result.IsSuccess)
            await _auditService.LogAsync("Club", clubId, "Archive", null, null, clubId, null, "CLB được lưu trữ");
        return result;
    }

    public async Task<ApiResult<bool>> ReopenClubAsync(Guid clubId)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("CLB không tồn tại.");
        if (club.Status == ClubStatus.Active)
            return ApiResult<bool>.Failure("CLB đang ở trạng thái Active rồi.");

        club.Status = ClubStatus.Active;
        club.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("Club", clubId, "Reopen", null, null, clubId, null, "CLB được mở lại");
        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> DissolveClubAsync(Guid clubId)
    {
        var result = await ChangeStatusAsync(clubId, ClubStatus.Deleted);
        if (result.IsSuccess)
            await _auditService.LogAsync("Club", clubId, "Dissolve", null, null, clubId, null, "CLB bị giải tán");
        return result;
    }

    public async Task<ApiResult<bool>> DeleteClubAsync(Guid clubId, bool hardDelete = false)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("CLB không tồn tại.");

        if (hardDelete)
        {
            _uow.Clubs.Remove(club);
            await _auditService.LogAsync("Club", clubId, "HardDelete", null, null, null, null, "CLB bị xóa cứng");
        }
        else
        {
            club.Status = ClubStatus.Deleted;
            club.UpdatedAt = DateTime.UtcNow;
            await _auditService.LogAsync("Club", clubId, "SoftDelete", null, null, clubId, null, "CLB bị xóa mềm");
        }

        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }

    public async Task<PagedResult<ClubSummaryDto>> GetMyClubsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Clubs.QueryMyClubs(userId);

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

    /// <summary>Lấy danh sách người dùng có Role = ClubAdmin để UniAdmin chọn khi tạo CLB</summary>
    public async Task<List<UserProfileDto>> GetClubAdminsAsync()
    {
        return await _uow.Users.Query()
            .Where(u => u.Role == Role.ClubAdmin && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new UserProfileDto(
                u.Id, u.FullName, u.Username, u.Email,
                u.StudentCode, u.Phone, u.AvatarUrl,
                u.Role.ToString(), u.Status.ToString(), u.IsEmailVerified, u.CreatedAt))
            .ToListAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.IsClubAdminAsync(clubId, userId);

    private async Task<ApiResult<bool>> ChangeStatusAsync(Guid clubId, ClubStatus status)
    {
        var club = await _uow.Clubs.GetByIdAsync(clubId);
        if (club == null) return ApiResult<bool>.Failure("CLB không tồn tại.");
        club.Status = status;
        club.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ApiResult<bool>.Success(true);
    }
}
