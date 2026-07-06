using ClubHub.API.DTOs.Feedback;
using ClubHub.API.Entities;
using ClubHub.API.Enums;
using ClubHub.API.Repositories;
using ClubHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClubHub.API.Services.Interfaces;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _uow;
    private readonly IPointService _pointService;

    public FeedbackService(IUnitOfWork uow, IPointService pointService)
    {
        _uow = uow;
        _pointService = pointService;
    }

    public async Task<ApiResult<FeedbackDto>> SubmitFeedbackAsync(Guid eventId, Guid userId, SubmitFeedbackRequest req)
    {
        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) return ApiResult<FeedbackDto>.Failure("Sự kiện không tồn tại.");
        if (ev.Status != EventStatus.Completed)
            return ApiResult<FeedbackDto>.Failure("Chỉ có thể gửi feedback cho sự kiện đã kết thúc.");

        var reg = await _uow.EventRegistrations.FirstOrDefaultAsync(r =>
            r.EventId == eventId && r.UserId == userId && r.IsCheckedIn);
        if (reg == null)
            return ApiResult<FeedbackDto>.Failure("Bạn cần check-in sự kiện trước khi gửi feedback.");

        if (await _uow.Feedbacks.AnyAsync(f => f.EventId == eventId && f.UserId == userId))
            return ApiResult<FeedbackDto>.Failure("Bạn đã gửi feedback cho sự kiện này rồi.");

        var feedback = new Feedback
        {
            EventId = eventId,
            UserId = userId,
            Rating = req.Rating,
            Comment = req.Comment
        };

        _uow.Feedbacks.Add(feedback);
        await _uow.SaveChangesAsync();

        // Award 2 points for feedback
        await _pointService.AddPointsAsync(userId, ev.ClubId, 2, PointType.Feedback,
            $"Gửi feedback sự kiện: {ev.Name}", eventId);

        var user = await _uow.Users.GetByIdAsync(userId);
        return ApiResult<FeedbackDto>.Success(new FeedbackDto(
            feedback.Id, userId, user!.FullName, req.Rating, req.Comment, feedback.CreatedAt));
    }

    public async Task<FeedbackSummaryDto> GetEventFeedbackAsync(Guid eventId)
    {
        var feedbacks = await _uow.Feedbacks.Query()
            .Include(f => f.User)
            .Where(f => f.EventId == eventId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var items = feedbacks.Select(f => new FeedbackDto(
            f.Id, f.UserId, f.User.FullName, f.Rating, f.Comment, f.CreatedAt)).ToList();

        var avg = feedbacks.Count > 0 ? feedbacks.Average(f => f.Rating) : 0;

        return new FeedbackSummaryDto(Math.Round(avg, 2), feedbacks.Count, items);
    }
}
