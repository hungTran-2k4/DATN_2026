using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using DATN.Application.Features.Me.Commands;
using DATN.Application.Features.Me.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN.api.Controllers;

/// <summary>
/// Controller cho profile và địa chỉ của người dùng đang đăng nhập
/// </summary>
[Route("api/me")]
[ApiController]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ──────────────── PROFILE ────────────────

    /// <summary>Lấy thông tin profile của user hiện tại</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetMyProfileQuery(GetCurrentUserId()));
        if (!result.Success) return result.StatusCode == 404 ? NotFound(result) : StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Đổi mật khẩu</summary>
    [HttpPut("change-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(GetCurrentUserId(), request.CurrentPassword, request.NewPassword));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ──────────────── ADDRESS BOOK ────────────────

    /// <summary>Lấy danh sách địa chỉ của user hiện tại</summary>
    [HttpGet("addresses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserAddressDto>>), 200)]
    public async Task<IActionResult> GetAddresses()
    {
        var result = await _mediator.Send(new GetMyAddressesQuery(GetCurrentUserId()));
        return Ok(result);
    }

    /// <summary>Thêm địa chỉ mới</summary>
    [HttpPost("addresses")]
    [ProducesResponseType(typeof(ApiResponse<UserAddressDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<UserAddressDto>), 400)]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressCommand command)
    {
        command.UserId = GetCurrentUserId();
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return StatusCode(201, result);
    }

    /// <summary>Cập nhật địa chỉ</summary>
    [HttpPut("addresses/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressCommand command)
    {
        command.Id = id;
        command.UserId = GetCurrentUserId();
        var result = await _mediator.Send(command);
        if (!result.Success)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Xóa địa chỉ (không thể xóa địa chỉ mặc định)</summary>
    [HttpDelete("addresses/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var result = await _mediator.Send(new DeleteAddressCommand(id, GetCurrentUserId()));
        if (!result.Success)
            return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Đặt địa chỉ làm mặc định</summary>
    [HttpPatch("addresses/{id:guid}/default")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<IActionResult> SetDefaultAddress(Guid id)
    {
        var result = await _mediator.Send(new SetDefaultAddressCommand(id, GetCurrentUserId()));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
