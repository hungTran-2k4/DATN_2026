using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using DATN.Application.Features.Orders.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/orders")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Checkout: Tạo đơn hàng từ giỏ hàng.
    /// 1 checkout → nhiều đơn (mỗi shop 1 đơn).
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderSummaryDto>>), 201)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderSummaryDto>>), 400)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command)
    {
        command.BuyerId = GetCurrentUserId();
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Lịch sử mua hàng của buyer hiện tại</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderSummaryDto>>), 200)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // TODO: implement GetMyOrdersQuery
        return Ok(ApiResponse<string>.Succeed("Not yet implemented"));
    }

    /// <summary>Chi tiết 1 đơn hàng (buyer hoặc seller)</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 404)]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        // TODO: implement GetOrderDetailQuery
        return Ok(ApiResponse<string>.Succeed("Not yet implemented"));
    }

    /// <summary>Buyer hủy đơn hàng (chỉ khi status = PENDING)</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 403)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, GetCurrentUserId(), request.Reason));
        if (!result.Success)
            return result.StatusCode switch
            {
                403 => StatusCode(403, result),
                404 => NotFound(result),
                _ => BadRequest(result)
            };
        return Ok(result);
    }

    /// <summary>Seller/Admin cập nhật trạng thái đơn hàng</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderStatusCommand
        {
            OrderId = id,
            ActorId = GetCurrentUserId(),
            NewStatus = request.NewStatus,
            Note = request.Note
        });
        if (!result.Success)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }
}

public class CancelOrderRequest { public string? Reason { get; set; } }
public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
}
