using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Reviews.Commands;
using DATN.Application.Features.Reviews.Queries;
using DATN.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/reviews")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStorageService _storageService;

    public ReviewsController(IMediator mediator, IStorageService storageService)
    {
        _mediator = mediator;
        _storageService = storageService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lấy danh sách đánh giá của 1 sản phẩm (Public)</summary>
    [HttpGet("/api/products/{productId:guid}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<ReviewDto>>), 200)]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetProductReviewsQuery(productId, page, pageSize));
        return Ok(result);
    }

    /// <summary>Lấy rating tổng quát của 1 sản phẩm (Public)</summary>
    [HttpGet("/api/products/{productId:guid}/reviews/rating")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductRatingDto>), 200)]
    public async Task<IActionResult> GetProductRating(Guid productId)
    {
        var result = await _mediator.Send(new GetProductRatingQuery(productId));
        return Ok(result);
    }

    /// <summary>Lấy danh sách đánh giá của user hiện tại</summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<ReviewDto>>), 200)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyReviewsQuery(GetCurrentUserId(), page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Tạo đánh giá mới (chỉ dành cho khách hàng đã mua).
    /// Hỗ trợ upload ảnh trực tiếp qua IFormFile[].
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), 400)]
    public async Task<IActionResult> CreateReview(
        [FromForm] CreateReviewRequest request,
        [FromForm] List<IFormFile>? images)
    {
        // Upload ảnh nếu có
        List<string>? imageUrls = null;
        if (images != null && images.Count > 0)
        {
            imageUrls = new List<string>();
            foreach (var file in images)
            {
                if (file.Length > 0)
                {
                    using var stream = file.OpenReadStream();
                    var url = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);
                    imageUrls.Add(url);
                }
            }
        }

        var command = new CreateReviewCommand
        {
            UserId = GetCurrentUserId(),
            VariantId = request.VariantId,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment,
            Images = imageUrls ?? request.ImageUrls // Hỗ trợ cả upload file và truyền URL trực tiếp
        };

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Xóa đánh giá của chính mình</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 403)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var result = await _mediator.Send(new DeleteReviewCommand(id, GetCurrentUserId()));
        if (!result.Success)
        {
            if (result.StatusCode == 404) return NotFound(result);
            if (result.StatusCode == 403) return StatusCode(403, result);
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public class CreateReviewRequest
{
    public Guid VariantId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    /// <summary>Truyền URL ảnh trực tiếp (fallback nếu không upload IFormFile)</summary>
    public List<string>? ImageUrls { get; set; }
}
