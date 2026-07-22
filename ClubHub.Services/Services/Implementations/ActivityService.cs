using ClubHub.API.DTOs.Activity;
using ClubHub.API.DTOs.Common;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Implementations;

public class ActivityService : IActivityService
{
    private readonly IUnitOfWork _uow;
    private readonly IPointService _pointService;
    private readonly IAuditService _auditService;

    public ActivityService(IUnitOfWork uow, IPointService pointService, IAuditService auditService)
    {
        _uow = uow;
        _pointService = pointService;
        _auditService = auditService;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CRUD
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<PagedResult<ActivityDto>> GetClubActivitiesAsync(Guid clubId, int page, int pageSize)
    {
        var query = _uow.Activities.Query()
            .Include(a => a.Club)
            .Include(a => a.Creator)
            .Include(a => a.Registrations)
            .Where(a => a.ClubId == clubId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new PagedResult<ActivityDto>(items, page, pageSize, total);
    }

    public async Task<ActivityDetailDto?> GetActivityByIdAsync(Guid activityId, Guid? currentUserId)
    {
        var activity = await _uow.Activities.Query()
            .Include(a => a.Club)
            .Include(a => a.Creator)
            .Include(a => a.Registrations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return null;

        // Chỉ include registrants nếu currentUser là ClubAdmin
        List<ActivityRegistrantDto>? registrants = null;
        if (currentUserId.HasValue)
        {
            var isAdmin = await IsClubAdminAsync(activity.ClubId, currentUserId.Value);
            if (isAdmin)
            {
                registrants = activity.Registrations
                    .Select(r => new ActivityRegistrantDto(
                        r.Id, r.UserId, r.User.FullName, r.User.AvatarUrl,
                        r.User.StudentCode, r.IsCheckedIn, r.CheckInTime,
                        r.Note, r.RegisteredAt))
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToList();
            }
        }

        return MapToDetailDto(activity, registrants);
    }

    public async Task<ApiResult<ActivityDto>> CreateActivityAsync(Guid clubId, CreateActivityRequest req, Guid createdBy)
    {
        if (!await IsClubAdminAsync(clubId, createdBy))
            return ApiResult<ActivityDto>.Failure("Chỉ Club Admin mới được tạo hoạt động.");

        if (req.StartTime >= req.EndTime)
            return ApiResult<ActivityDto>.Failure("Thời gian kết thúc phải sau thời gian bắt đầu.");

        if (req.RegistrationDeadline.HasValue && req.RegistrationDeadline >= req.StartTime)
            return ApiResult<ActivityDto>.Failure("Hạn đăng ký phải trước thời gian bắt đầu.");

        var activity = new ClubActivity
        {
            ClubId = clubId,
            Title = req.Title,
            Description = req.Description,
            Type = req.Type,
            Location = req.Location,
            ImageUrl = req.ImageUrl,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            RegistrationDeadline = req.RegistrationDeadline,
            Capacity = req.Capacity,
            CheckInPoints = req.CheckInPoints,
            Status = ActivityStatus.Upcoming,
            CreatedBy = createdBy
        };

        _uow.Activities.Add(activity);
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubActivity", activity.Id, "Create",
            createdBy, null, clubId, null, $"Tạo hoạt động: {activity.Title}");

        var dto = await GetActivityDtoByIdAsync(activity.Id);
        return ApiResult<ActivityDto>.Success(dto!);
    }

    public async Task<ApiResult<ActivityDto>> UpdateActivityAsync(Guid activityId, UpdateActivityRequest req, Guid requesterId)
    {
        var activity = await _uow.Activities.Query()
            .Include(a => a.Club)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null)
            return ApiResult<ActivityDto>.Failure("Hoạt động không tồn tại.");

        if (!await IsClubAdminAsync(activity.ClubId, requesterId))
            return ApiResult<ActivityDto>.Failure("Bạn không có quyền chỉnh sửa hoạt động này.");

        if (req.Title != null) activity.Title = req.Title;
        if (req.Description != null) activity.Description = req.Description;
        if (req.Type != null) activity.Type = req.Type;
        if (req.Location != null) activity.Location = req.Location;
        if (req.ImageUrl != null) activity.ImageUrl = req.ImageUrl;
        if (req.StartTime.HasValue) activity.StartTime = req.StartTime.Value;
        if (req.EndTime.HasValue) activity.EndTime = req.EndTime.Value;
        if (req.RegistrationDeadline.HasValue) activity.RegistrationDeadline = req.RegistrationDeadline;
        if (req.Capacity.HasValue) activity.Capacity = req.Capacity;
        if (req.CheckInPoints.HasValue) activity.CheckInPoints = req.CheckInPoints.Value;
        if (req.Status.HasValue) activity.Status = req.Status.Value;
        activity.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubActivity", activityId, "Update",
            requesterId, null, activity.ClubId, null, $"Cập nhật hoạt động: {activity.Title}");

        var dto = await GetActivityDtoByIdAsync(activityId);
        return ApiResult<ActivityDto>.Success(dto!);
    }

    public async Task<ApiResult<bool>> DeleteActivityAsync(Guid activityId, Guid requesterId)
    {
        var activity = await _uow.Activities.GetByIdAsync(activityId);
        if (activity == null)
            return ApiResult<bool>.Failure("Hoạt động không tồn tại.");

        if (!await IsClubAdminAsync(activity.ClubId, requesterId))
            return ApiResult<bool>.Failure("Bạn không có quyền xóa hoạt động này.");

        activity.Status = ActivityStatus.Cancelled;
        activity.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _auditService.LogAsync("ClubActivity", activityId, "Cancel",
            requesterId, null, activity.ClubId, null, $"Hủy hoạt động: {activity.Title}");

        return ApiResult<bool>.Success(true);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Registration
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<ApiResult<bool>> RegisterAsync(Guid activityId, Guid userId, RegisterActivityRequest? request)
    {
        var activity = await _uow.Activities.Query()
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null)
            return ApiResult<bool>.Failure("Hoạt động không tồn tại.");

        if (activity.Status != ActivityStatus.Upcoming)
            return ApiResult<bool>.Failure("Hoạt động này không còn nhận đăng ký.");

        // Kiểm tra hạn đăng ký
        if (activity.RegistrationDeadline.HasValue && DateTime.UtcNow > activity.RegistrationDeadline.Value)
            return ApiResult<bool>.Failure("Đã hết hạn đăng ký tham gia hoạt động này.");

        // Kiểm tra đã đăng ký chưa
        var existing = activity.Registrations
            .FirstOrDefault(r => r.UserId == userId && !r.IsCancelled);
        if (existing != null)
            return ApiResult<bool>.Failure("Bạn đã đăng ký tham gia hoạt động này rồi.");

        // Kiểm tra sức chứa
        var activeCount = activity.Registrations.Count(r => !r.IsCancelled);
        if (activity.Capacity.HasValue && activeCount >= activity.Capacity.Value)
            return ApiResult<bool>.Failure("Hoạt động này đã đủ số lượng người tham gia.");

        var registration = new ActivityRegistration
        {
            ActivityId = activityId,
            UserId = userId,
            Note = request?.Note,
            RegisteredAt = DateTime.UtcNow
        };

        _uow.ActivityRegistrations.Add(registration);
        await _uow.SaveChangesAsync();

        return ApiResult<bool>.Success(true);
    }

    public async Task<ApiResult<bool>> CancelRegistrationAsync(Guid activityId, Guid userId)
    {
        var activity = await _uow.Activities.GetByIdAsync(activityId);
        if (activity == null)
            return ApiResult<bool>.Failure("Hoạt động không tồn tại.");

        var registration = await _uow.ActivityRegistrations
            .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.UserId == userId && !r.IsCancelled);

        if (registration == null)
            return ApiResult<bool>.Failure("Bạn chưa đăng ký hoạt động này.");

        registration.IsCancelled = true;
        registration.CancelledAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return ApiResult<bool>.Success(true);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Check-in
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<ApiResult<bool>> CheckInAsync(Guid activityId, Guid userId, Guid checkinById)
    {
        var activity = await _uow.Activities.GetByIdAsync(activityId);
        if (activity == null)
            return ApiResult<bool>.Failure("Hoạt động không tồn tại.");

        // Chỉ ClubAdmin mới check-in được
        if (!await IsClubAdminAsync(activity.ClubId, checkinById))
            return ApiResult<bool>.Failure("Chỉ Club Admin mới được check-in cho thành viên.");

        var registration = await _uow.ActivityRegistrations
            .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.UserId == userId && !r.IsCancelled);

        if (registration == null)
            return ApiResult<bool>.Failure("Thành viên này chưa đăng ký hoạt động.");

        if (registration.IsCheckedIn)
            return ApiResult<bool>.Failure("Thành viên này đã được check-in trước đó.");

        registration.IsCheckedIn = true;
        registration.CheckInTime = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        // Tích điểm cho member
        await _pointService.AddPointsAsync(
            userId, activity.ClubId, activity.CheckInPoints,
            PointType.CheckIn,
            $"Check-in hoạt động: {activity.Title}",
            activityId);

        await _auditService.LogAsync("ActivityRegistration", registration.Id, "CheckIn",
            checkinById, null, activity.ClubId, null,
            $"User {userId} checked in for activity {activityId}");

        return ApiResult<bool>.Success(true);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // My Activities
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<PagedResult<MyRegisteredActivityDto>> GetMyRegisteredActivitiesAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.ActivityRegistrations.Query()
            .Include(r => r.Activity)
                .ThenInclude(a => a.Club)
            .Where(r => r.UserId == userId && !r.IsCancelled);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.RegisteredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MyRegisteredActivityDto(
                r.Id, r.ActivityId, r.Activity.ClubId,
                r.Activity.Club.Name, r.Activity.Title, r.Activity.Type,
                r.Activity.Location, r.Activity.StartTime, r.Activity.EndTime,
                r.IsCheckedIn, r.CheckInTime,
                r.Activity.Status.ToString(), r.RegisteredAt))
            .ToListAsync();

        return new PagedResult<MyRegisteredActivityDto>(items, page, pageSize, total);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<bool> IsClubAdminAsync(Guid clubId, Guid userId)
        => await _uow.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.UserId == userId &&
            m.Status == MembershipStatus.Approved &&
            m.RoleInClub == Role.ClubAdmin);

    private async Task<ActivityDto?> GetActivityDtoByIdAsync(Guid activityId)
    {
        var activity = await _uow.Activities.Query()
            .Include(a => a.Club)
            .Include(a => a.Creator)
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        return activity == null ? null : MapToDto(activity);
    }

    private static ActivityDto MapToDto(ClubActivity a) => new(
        a.Id, a.ClubId, a.Club?.Name ?? "", a.Title, a.Description,
        a.Type, a.Location, a.ImageUrl,
        a.StartTime, a.EndTime, a.RegistrationDeadline, a.Capacity,
        a.Registrations?.Count(r => !r.IsCancelled) ?? 0,
        a.Status.ToString(), a.Creator?.FullName, a.CreatedAt);

    private static ActivityDetailDto MapToDetailDto(ClubActivity a, List<ActivityRegistrantDto>? registrants) => new(
        a.Id, a.ClubId, a.Club?.Name ?? "", a.Title, a.Description,
        a.Type, a.Location, a.ImageUrl,
        a.StartTime, a.EndTime, a.RegistrationDeadline, a.Capacity,
        a.Registrations?.Count(r => !r.IsCancelled) ?? 0,
        a.Registrations?.Count(r => r.IsCheckedIn) ?? 0,
        a.Status.ToString(), a.CheckInPoints, a.Creator?.FullName,
        a.CreatedAt, a.UpdatedAt, registrants);
}
