using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands.CreateProduct;
using DATN.Application.Features.Products.Commands.DeleteProduct;
using DATN.Application.Features.Products.Commands.UpdateProduct;
using DATN.Application.Features.Products.Queries.GetProductById;
using DATN.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Có thể thêm [Authorize(Roles = "Admin")] nếu chỉ Admin được quản lý Product
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm (hỗ trợ phân trang, tìm kiếm)
    /// </summary>
    [HttpPost("paging")]
    [AllowAnonymous] // Cho phép khách xem sản phẩm
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromBody] GetProductsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết 1 sản phẩm
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, [FromQuery] Guid? shopId)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id, shopId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Thêm mới sản phẩm
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật sản phẩm
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command, [FromQuery] Guid? shopId)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<bool>.Fail("ID mismatch"));
        }

        // Tùy chọn nếu bạn muốn client phải báo rõ sửa cho shop nào qua querystring,
        // nếu không có querystring thì dùng command.ShopId.
        if (shopId.HasValue && command.ShopId != shopId.Value)
        {
            // Ép đồng bộ nếu truyền trên query
            command.ShopId = shopId.Value;
        }

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Xóa sản phẩm
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, [FromQuery] Guid? shopId)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id, shopId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}
