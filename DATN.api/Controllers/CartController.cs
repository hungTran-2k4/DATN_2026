using DATN.Application.Common.Models;
using DATN.Application.DTOs.Cart;
using DATN.Application.Features.Cart.Commands;
using DATN.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/cart")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    public CartController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lấy giỏ hàng của user (group theo Shop)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), 200)]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetMyCartQuery(GetCurrentUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ.
    /// Nếu variant đã có trong giỏ → cộng dồn số lượng.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<CartItemDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CartItemDto>), 400)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command)
    {
        command.UserId = GetCurrentUserId();
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Cập nhật số lượng sản phẩm trong giỏ</summary>
    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        var result = await _mediator.Send(new UpdateCartItemCommand(cartItemId, GetCurrentUserId(), request.Quantity));
        if (!result.Success)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Xóa 1 sản phẩm khỏi giỏ</summary>
    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(cartItemId, GetCurrentUserId()));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Xóa toàn bộ giỏ hàng</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand(GetCurrentUserId()));
        return Ok(result);
    }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}
