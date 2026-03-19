using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands;
using DATN.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StocksController : ControllerBase
{
    private readonly IMediator _mediator;

    public StocksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("variant/{variantId:guid}")]
    public async Task<ActionResult<ApiResponse<StockDto>>> GetByVariantId(Guid variantId)
    {
        var result = await _mediator.Send(new GetStockByVariantIdQuery(variantId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockDto>>>> GetByProductId(Guid productId)
    {
        var result = await _mediator.Send(new GetStocksByProductQuery(productId));
        return Ok(result);
    }

    [HttpGet("transactions/variant/{variantId:guid}")]
    public async Task<ActionResult<PagedResponse<IEnumerable<StockTransactionDto>>>> GetTransactionsByVariant(
        Guid variantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStockTransactionsByVariantQuery(variantId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("transactions/shop/{shopId:guid}")]
    public async Task<ActionResult<PagedResponse<IEnumerable<StockTransactionDto>>>> GetTransactionsByShop(
        Guid shopId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStockTransactionsByShopQuery(shopId, page, pageSize));
        return Ok(result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<StockDto>>> UpdateStock([FromBody] UpdateStockCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("restock")]
    public async Task<ActionResult<ApiResponse<bool>>> Restock([FromBody] RestockCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
