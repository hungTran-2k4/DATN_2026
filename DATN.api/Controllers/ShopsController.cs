using DATN.Application.Common.Models;
using DATN.Application.DTOs.Shops;
using DATN.Application.Features.Shops.Commands.CreateShop;
using DATN.Application.Features.Shops.Commands.UpdateShop;
using DATN.Application.Features.Shops.Commands.DeleteShop;
using DATN.Application.Features.Shops.Queries.GetShops;
using DATN.Application.Features.Shops.Queries.GetShopById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Hầu hết thao tác của cửa hàng yêu cầu đăng nhập
public class ShopsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShopsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid? GetCurrentUserId()
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(nameIdentifier, out Guid userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ Shop
    /// </summary>
    [HttpGet]
    [AllowAnonymous] // Cho phép tất cả xem danh sách cửa hàng
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShopDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShops()
    {
        var result = await _mediator.Send(new GetShopsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách cửa hàng CỦA TÔI (người đang đăng nhập)
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShopDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShopDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyShops()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(ApiResponse<IEnumerable<ShopDto>>.Fail("User not authenticated"));

        var result = await _mediator.Send(new DATN.Application.Features.Shops.Queries.GetMyShops.GetMyShopsQuery(userId.Value));
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết Shop
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShopDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShopDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopById(Guid id)
    {
        var result = await _mediator.Send(new GetShopByIdQuery(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới cửa hàng
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopCommand command)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(ApiResponse<Guid>.Fail("User not authenticated"));

        // Gán cứng OwnerId bằng user đang đăng nhập để tránh tạo giùm người khác.
        command.OwnerId = userId;

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin cửa hàng
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShop(Guid id, [FromBody] UpdateShopCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<bool>.Fail("ID mismatch"));
        }

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(ApiResponse<bool>.Fail("User not authenticated"));

        // Bắt buộc quyền sở hữu (Owner)
        command.OwnerId = userId;

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            if (result.StatusCode == 404) return NotFound(result);
            if (result.StatusCode == 403) return StatusCode(403, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Xóa cửa hàng
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteShop(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(ApiResponse<bool>.Fail("User not authenticated"));

        var result = await _mediator.Send(new DeleteShopCommand(id, userId));
        
        if (!result.Success)
        {
             if (result.StatusCode == 404) return NotFound(result);
             if (result.StatusCode == 403) return StatusCode(403, result);
             return BadRequest(result);
        }
        return Ok(result);
    }
}
