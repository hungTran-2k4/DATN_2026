using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands.CreateProduct;
using DATN.Application.Features.Products.Commands.DeleteProduct;
using DATN.Application.Features.Products.Commands.UpdateProduct;
using DATN.Application.Features.Products.Commands;
using DATN.Application.Features.Products.Queries.GetProductById;
using DATN.Application.Features.Products.Queries.GetProducts;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

public class ReviewProductRequest
{
    /// <summary>"approve" hoặc "reject"</summary>
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
}

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

        if (User.IsInRole("Admin"))
        {
            command.BypassStatusCheck = true;
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
    /// Seller gửi sản phẩm để Admin duyệt (Draft → Pending)
    /// </summary>
    [HttpPut("{id}/submit-for-review")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview(Guid id, [FromQuery] Guid? shopId)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id, shopId));
        if (!product.Success || product.Data == null)
            return NotFound(ApiResponse<bool>.Fail("Sản phẩm không tồn tại.", 404));

        if (product.Data.Status != ProductStatus.Draft.ToStatusString() && product.Data.Status != ProductStatus.Rejected.ToStatusString())
            return BadRequest(ApiResponse<bool>.Fail("Chỉ có thể gửi duyệt sản phẩm ở trạng thái Nháp hoặc Bị từ chối.", 400, "INVALID_STATUS"));

        var command = new UpdateProductCommand
        {
            Id = id,
            Name = product.Data.Name ?? string.Empty,
            Sku = product.Data.Sku ?? string.Empty,
            Slug = product.Data.Slug ?? string.Empty,
            Description = product.Data.Description,
            Summary = product.Data.Summary,
            Status = ProductStatus.Pending.ToStatusString(),
            BrandId = product.Data.BrandId,
            CategoryId = product.Data.CategoryId,
            ShopId = shopId ?? product.Data.ShopId,
            BaseAttributes = product.Data.BaseAttributes,
        };
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(ApiResponse<bool>.Succeed(true, "Đã gửi sản phẩm để Admin duyệt."));
    }

    /// <summary>
    /// Admin duyệt hoặc từ chối sản phẩm (Pending → Active / Rejected)
    /// </summary>
    [HttpPut("{id}/review")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewProduct(Guid id, [FromBody] ReviewProductRequest request)
    {
        if (request.Action != "approve" && request.Action != "reject")
            return BadRequest(ApiResponse<bool>.Fail("Action phải là 'approve' hoặc 'reject'.", 400));

        var product = await _mediator.Send(new GetProductByIdQuery(id, null));
        if (!product.Success || product.Data == null)
            return NotFound(ApiResponse<bool>.Fail("Sản phẩm không tồn tại.", 404));

        var newStatus = request.Action == "approve"
            ? ProductStatus.Active.ToStatusString()
            : ProductStatus.Rejected.ToStatusString();
        var command = new UpdateProductCommand
        {
            Id = id,
            Name = product.Data.Name ?? string.Empty,
            Sku = product.Data.Sku ?? string.Empty,
            Slug = product.Data.Slug ?? string.Empty,
            Description = product.Data.Description,
            Summary = product.Data.Summary,
            Status = newStatus,
            BrandId = product.Data.BrandId,
            CategoryId = product.Data.CategoryId,
            ShopId = product.Data.ShopId,
            BaseAttributes = product.Data.BaseAttributes,
            BypassStatusCheck = true,
        };
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);

        var msg = request.Action == "approve" ? "Sản phẩm đã được duyệt và hiển thị." : "Sản phẩm đã bị từ chối.";
        return Ok(ApiResponse<bool>.Succeed(true, msg));
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
