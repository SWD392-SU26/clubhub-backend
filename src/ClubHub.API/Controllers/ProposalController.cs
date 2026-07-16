using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/proposals")]
[Route("api/club-proposals")]
[Authorize]
[Produces("application/json")]
public class ProposalController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public ProposalController(IProposalService proposalService)
        => _proposalService = proposalService;

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitProposalRequest request)
    {
        var result = await _proposalService.SubmitAsync(request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _proposalService.GetMyProposalsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _proposalService.GetByIdAsync(id);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Proposal does not exist."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProposalRequest request)
    {
        var result = await _proposalService.UpdateAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPut("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid id)
    {
        var result = await _proposalService.ResubmitAsync(id, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpGet]
    [Authorize(Roles = "UniversityAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _proposalService.GetAllAsync(status, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "UniversityAdmin")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewProposalRequest request)
    {
        var result = await _proposalService.ReviewAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPut("{id:guid}/request-revision")]
    [HttpPut("{id:guid}/request-info")]
    [Authorize(Roles = "UniversityAdmin")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RequestRevisionRequest request)
    {
        var result = await _proposalService.RequestRevisionAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
