using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Brands.Commands;
using DATN.Application.Features.Brands.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/brands")]
[ApiController]
public class BrandsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BrandsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lấy danh sách thương hiệu (phân trang, tìm kiếm) - Public</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<BrandDto>>), 200)]
    public async Task<IActionResult> GetBrands(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetBrandsQuery(search, page, pageSize));
        return Ok(result);
    }

    /// <summary>Lấy tất cả thương hiệu đang hoạt động (không phân trang) - Phục vụ Dropdown filter Public</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BrandDto>>), 200)]
    public async Task<IActionResult> GetActiveBrands()
    {
        var result = await _mediator.Send(new GetAllActiveBrandsQuery());
        return Ok(result);
    }

    /// <summary>Lấy chi tiết 1 thương hiệu - Public</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), 404)]
    public async Task<IActionResult> GetBrandById(Guid id)
    {
        var result = await _mediator.Send(new GetBrandByIdQuery(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Tạo mới thương hiệu - Chỉ Admin</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), 400)]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật thương hiệu - Chỉ Admin</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandCommand command)
    {
        command.Id = id; // Đảm bảo ID từ URL
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            if (result.StatusCode == 404) return NotFound(result);
            return BadRequest(result); // SLUG_EXISTS etc.
        }
        return Ok(result);
    }

    /// <summary>Xóa thương hiệu (hoặc ẩn nếu đang có sản phẩm) - Chỉ Admin</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> DeleteBrand(Guid id)
    {
        var result = await _mediator.Send(new DeleteBrandCommand(id));
        if (!result.Success)
        {
             if (result.StatusCode == 404) return NotFound(result);
             return StatusCode(500, result);
        }
        return Ok(result);
    }
}
