using ClubHub.API.DTOs.Feedback;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Interfaces;

public interface IFeedbackService
{
    Task<ApiResult<FeedbackDto>> SubmitFeedbackAsync(Guid eventId, Guid userId, SubmitFeedbackRequest request);
    Task<FeedbackSummaryDto> GetEventFeedbackAsync(Guid eventId);
}
