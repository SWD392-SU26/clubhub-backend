using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Feedback;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/feedback")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
        => _feedbackService = feedbackService;

    /// <summary>[Club Member] Gửi feedback sau sự kiện</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit(Guid eventId, [FromBody] SubmitFeedbackRequest request)
    {
        var result = await _feedbackService.SubmitFeedbackAsync(eventId, GetUserId(), request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!, "Feedback đã được ghi nhận. +2 điểm!"))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>Xem tổng hợp feedback của sự kiện</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFeedback(Guid eventId)
    {
        var result = await _feedbackService.GetEventFeedbackAsync(eventId);
        return Ok(ApiResponse.Ok(result));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
