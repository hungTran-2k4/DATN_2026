using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands.CreateProduct;
using DATN.Application.Features.Products.Commands.DeleteProduct;
using DATN.Application.Features.Products.Commands.UpdateProduct;
using DATN.Application.Features.Products.Commands;
using DATN.Application.Features.Products.Queries.GetProductById;
using DATN.Application.Features.Products.Queries.GetProducts;
using DATN.Application.Interfaces.Services;
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
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStorageService _storageService;

    public ProductsController(IMediator mediator, IStorageService storageService)
    {
        _mediator = mediator;
        _storageService = storageService;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm (hỗ trợ phân trang, tìm kiếm)
    /// </summary>
    [HttpPost("paging")]
    [AllowAnonymous]
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

        if (shopId.HasValue && command.ShopId != shopId.Value)
        {
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

    /// <summary>
    /// Thêm ảnh cho sản phẩm (IFormFile → Azure Blob → lưu URL vào DB)
    /// </summary>
    [HttpPost("{id}/images")]
    [ProducesResponseType(typeof(ApiResponse<ProductImageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductImageDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddImage(Guid id, IFormFile file, [FromQuery] bool isMain = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<ProductImageDto>.Fail("Vui lòng chọn file ảnh.", 400, "NO_FILE"));

        // Upload lên Azure Blob Storage
        using var stream = file.OpenReadStream();
        var imageUrl = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        var command = new UploadProductImageCommand
        {
            ProductId = id,
            ImageUrl = imageUrl,
            IsMain = isMain
        };
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Xóa ảnh sản phẩm
    /// </summary>
    [HttpDelete("{id}/images/{imageId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var result = await _mediator.Send(new DeleteProductImageCommand(id, imageId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Đặt ảnh làm đại diện (Main image)
    /// </summary>
    [HttpPut("{id}/images/{imageId}/main")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMainImage(Guid id, Guid imageId)
    {
        var result = await _mediator.Send(new SetMainProductImageCommand(id, imageId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
