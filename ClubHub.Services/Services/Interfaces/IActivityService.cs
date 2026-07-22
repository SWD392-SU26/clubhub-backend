using ClubHub.API.DTOs.Activity;
using ClubHub.API.DTOs.Common;

namespace ClubHub.API.Services.Interfaces;

public interface IActivityService
{
    // ── CRUD ────────────────────────────────────────────────────────────────
    Task<PagedResult<ActivityDto>> GetClubActivitiesAsync(Guid clubId, int page, int pageSize);
    Task<ActivityDetailDto?> GetActivityByIdAsync(Guid activityId, Guid? currentUserId);
    Task<ApiResult<ActivityDto>> CreateActivityAsync(Guid clubId, CreateActivityRequest request, Guid createdBy);
    Task<ApiResult<ActivityDto>> UpdateActivityAsync(Guid activityId, UpdateActivityRequest request, Guid requesterId);
    Task<ApiResult<bool>> DeleteActivityAsync(Guid activityId, Guid requesterId);

    // ── Registration ────────────────────────────────────────────────────────
    Task<ApiResult<bool>> RegisterAsync(Guid activityId, Guid userId, RegisterActivityRequest? request);
    Task<ApiResult<bool>> CancelRegistrationAsync(Guid activityId, Guid userId);

    // ── Check-in ────────────────────────────────────────────────────────────
    Task<ApiResult<bool>> CheckInAsync(Guid activityId, Guid userId, Guid checkinById);

    // ── My Activities ───────────────────────────────────────────────────────
    Task<PagedResult<MyRegisteredActivityDto>> GetMyRegisteredActivitiesAsync(Guid userId, int page, int pageSize);
}
