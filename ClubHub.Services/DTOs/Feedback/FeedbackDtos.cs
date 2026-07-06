using System.ComponentModel.DataAnnotations;

namespace ClubHub.API.DTOs.Feedback;

public record SubmitFeedbackRequest(
    [Required, Range(1, 5)] int Rating,
    [MaxLength(1000)] string? Comment
);

public record FeedbackDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record FeedbackSummaryDto(
    double AverageRating,
    int TotalCount,
    List<FeedbackDto> Items
);
