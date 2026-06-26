using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/admin/club-proposals")]
[Authorize(Roles = "UniversityAdmin")]
[Produces("application/json")]
public class AdminClubProposalsController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public AdminClubProposalsController(IProposalService proposalService)
        => _proposalService = proposalService;

    /// <summary>[University Admin] View all club creation proposals</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _proposalService.GetAllAsync(status, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>[University Admin] Approve or reject a club creation proposal</summary>
    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewProposalRequest request)
    {
        var result = await _proposalService.ReviewAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[University Admin] Request more information for a proposal</summary>
    [HttpPut("{id:guid}/request-info")]
    public async Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestRevisionRequest request)
    {
        var result = await _proposalService.RequestRevisionAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
