using DATN.Application.Common.Models;
using DATN.Application.Features.Images.Commands.UploadImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Require authentication to upload images
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ImagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Upload file ảnh lên hệ thống Azure Blob Storage
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("No file uploaded."));
        }

        using var stream = file.OpenReadStream();
        var command = new UploadImageCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var response = await _mediator.Send(command);

        if (response.Success)
        {
            return Ok(response);
        }

        return BadRequest(response); // Return 400 with the error response
    }
}
