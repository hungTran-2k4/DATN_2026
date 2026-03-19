using DATN.Application.Common.Models;
using DATN.Application.Features.Images.Commands.UploadImage;
using DATN.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStorageService _storageService;

    public ImagesController(IMediator mediator, IStorageService storageService)
    {
        _mediator = mediator;
        _storageService = storageService;
    }

    /// <summary>
    /// Upload 1 file ảnh lên Azure Blob Storage
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

        return BadRequest(response);
    }

    /// <summary>
    /// Upload nhiều file ảnh cùng lúc, trả về danh sách URL
    /// </summary>
    [HttpPost("upload-multiple")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(ApiResponse<List<string>>.Fail("Vui lòng chọn ít nhất 1 file ảnh."));

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var url = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);
                urls.Add(url);
            }
        }

        return Ok(ApiResponse<List<string>>.Succeed(urls, $"Đã upload thành công {urls.Count} ảnh."));
    }
}
