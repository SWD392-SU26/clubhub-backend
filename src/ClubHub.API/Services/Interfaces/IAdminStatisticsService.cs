using ClubHub.API.DTOs.Admin;

namespace ClubHub.API.Services.Interfaces;

public interface IAdminStatisticsService
{
    Task<UniversityStatisticsDto> GetUniversityStatisticsAsync();
    Task<ClubStatisticsDto?> GetClubStatisticsAsync(Guid clubId);
}
