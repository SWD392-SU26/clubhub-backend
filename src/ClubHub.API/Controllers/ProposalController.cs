using System.Security.Claims;
using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Enums;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/proposals")]
[Authorize]
[Produces("application/json")]
public class ProposalController : ControllerBase
{
    private readonly IProposalService _proposalService;

    public ProposalController(IProposalService proposalService)
        => _proposalService = proposalService;

    /// <summary>[Student] Nộp hồ sơ thành lập CLB</summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitProposalRequest request)
    {
        var result = await _proposalService.SubmitAsync(request, GetUserId());
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[Student] Xem hồ sơ của mình</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _proposalService.GetMyProposalsAsync(GetUserId());
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Xem chi tiết hồ sơ</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _proposalService.GetByIdAsync(id);
        return result != null
            ? Ok(ApiResponse.Ok(result))
            : NotFound(ApiResponse.Fail("Hồ sơ không tồn tại."));
    }

    /// <summary>[University Admin] Xem tất cả hồ sơ</summary>
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

    /// <summary>[University Admin] Duyệt hoặc từ chối hồ sơ</summary>
    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "UniversityAdmin")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewProposalRequest request)
    {
        var result = await _proposalService.ReviewAsync(id, request, GetUserId());
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    /// <summary>[University Admin] Yêu cầu bổ sung hồ sơ</summary>
    [HttpPut("{id:guid}/request-revision")]
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
