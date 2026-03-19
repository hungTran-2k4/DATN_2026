using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Wishlists.Commands;
using DATN.Application.Features.Wishlists.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/wishlists")]
[ApiController]
[Authorize]
public class WishlistsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WishlistsController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lấy danh sách sản phẩm yêu thích của user (có phân trang)</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<WishlistItemDto>>), 200)]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyWishlistQuery(GetCurrentUserId(), page, pageSize));
        return Ok(result);
    }

    /// <summary>Kiểm tra xem sản phẩm đã có trong wishlist chưa</summary>
    [HttpGet("{productId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> CheckStatus(Guid productId)
    {
        var result = await _mediator.Send(new CheckWishlistStatusQuery(GetCurrentUserId(), productId));
        return Ok(result);
    }

    /// <summary>Thêm sản phẩm vào wishlist (Toogle kiểu Add)</summary>
    [HttpPost("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> AddToWishlist(Guid productId)
    {
        var result = await _mediator.Send(new AddToWishlistCommand(GetCurrentUserId(), productId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Xóa sản phẩm khỏi wishlist</summary>
    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId)
    {
        var result = await _mediator.Send(new RemoveFromWishlistCommand(GetCurrentUserId(), productId));
        return Ok(result);
    }
}
