using ClubHub.API.DTOs.Common;
using ClubHub.API.DTOs.Upload;
using ClubHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubHub.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;

    public UploadController(IUploadService uploadService)
        => _uploadService = uploadService;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadResponseDto>), 200)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string purpose = "general")
    {
        var result = await _uploadService.UploadAsync(file, purpose, Request);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error!));
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadAvatar([FromForm] IFormFile file) => Upload(file, "avatar");

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadLogo([FromForm] IFormFile file) => Upload(file, "logo");

    [HttpPost("cover")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadCover([FromForm] IFormFile file) => Upload(file, "cover");

    [HttpPost("proposal-file")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadProposalFile([FromForm] IFormFile file) => Upload(file, "proposal-file");

    [HttpPost("founder-id")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadFounderId([FromForm] IFormFile file) => Upload(file, "founder-id");
}
