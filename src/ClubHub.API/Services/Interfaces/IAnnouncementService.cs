using ClubHub.API.DTOs.Announcement;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetClubAnnouncementsAsync(Guid clubId, Guid requesterId, int page, int pageSize);
    Task<ApiResult<AnnouncementDto>> CreateAsync(Guid clubId, CreateAnnouncementRequest request, Guid requesterId);
    Task<ApiResult<AnnouncementDto>> UpdateAsync(Guid clubId, Guid announcementId, UpdateAnnouncementRequest request, Guid requesterId);
    Task<ApiResult<bool>> ArchiveAsync(Guid clubId, Guid announcementId, Guid requesterId);
}
