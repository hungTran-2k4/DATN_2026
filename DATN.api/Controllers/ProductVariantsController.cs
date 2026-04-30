using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.ProductVariants.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

[Route("api/products/{productId:guid}/variants")]
[ApiController]
public class ProductVariantsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductVariantsController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Tạo biến thể mới cho sản phẩm (Seller only)</summary>
    [HttpPost]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductVariantDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<ProductVariantDto>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ProductVariantDto>), 403)]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateVariantCommand command)
    {
        command.ProductId = productId;
        // ShopId phải được truyền trong body hoặc lấy từ shop của user
        var result = await _mediator.Send(command);
        if (!result.Success)
            return result.StatusCode == 403 ? StatusCode(403, result) : BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật biến thể (Seller only)</summary>
    [HttpPut("{variantId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 403)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> Update(Guid productId, Guid variantId, [FromBody] UpdateVariantCommand command)
    {
        command.Id = variantId;
        var result = await _mediator.Send(command);
        if (!result.Success)
            return result.StatusCode switch
            {
                403 => StatusCode(403, result),
                404 => NotFound(result),
                _ => BadRequest(result)
            };
        return Ok(result);
    }

    /// <summary>Xóa biến thể (Seller only)</summary>
    [HttpDelete("{variantId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId, [FromQuery] Guid shopId)
    {
        var result = await _mediator.Send(new DeleteVariantCommand(variantId, shopId, productId));
        if (!result.Success)
            return result.StatusCode == 403 ? StatusCode(403, result) : NotFound(result);
        return Ok(result);
    }

    /// <summary>Lưu hàng loạt biến thể (Bulk Upsert) có Transaction</summary>
    [HttpPost]
    [Route("variantsBulkSave")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 403)]
    public async Task<IActionResult> BulkSave(Guid productId, [FromBody] BulkSaveVariantsCommand command)
    {
        command.ProductId = productId;
        var result = await _mediator.Send(command);
        if (!result.Success)
            return result.StatusCode == 403 ? StatusCode(403, result) : BadRequest(result);
        return Ok(result);
    }
}
