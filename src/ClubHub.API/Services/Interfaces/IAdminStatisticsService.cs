using ClubHub.API.DTOs.Admin;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IAdminStatisticsService
{
    Task<UniversityStatisticsDto> GetUniversityStatisticsAsync();
    Task<ClubStatisticsDto?> GetClubStatisticsAsync(Guid clubId);
    Task<ApiResult<ClubStatisticsDto>> GetClubStatisticsForUserAsync(Guid clubId, Guid requesterId);
}
