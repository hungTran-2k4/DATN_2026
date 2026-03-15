using DATN.Application.Common.Models;
using DATN.Application.DTOs.Categories;
using DATN.Application.Features.Categories.Commands;
using DATN.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lấy danh mục dạng tree (cấp cha-con). Public API.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), 200)]
    public async Task<IActionResult> GetCategories([FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(activeOnly));
        return Ok(result);
    }

    /// <summary>Lấy chi tiết 1 danh mục</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Tạo danh mục mới (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật danh mục (Admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Ẩn danh mục (Admin only) — không xóa thực</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _mediator.Send(new DeactivateCategoryCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
