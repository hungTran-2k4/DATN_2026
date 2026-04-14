using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;
using DATN.Application.Features.Me.Commands;
using DATN.Application.Features.Me.Queries;
using DATN.Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private readonly IStorageService _storageService;

    public MeController(IMediator mediator, IStorageService storageService)
    {
        _mediator = mediator;
        _storageService = storageService;
    }

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

    /// <summary>Cập nhật profile (tên hiển thị, avatar URL)</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 404)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var command = new UpdateProfileCommand
        {
            UserId = GetCurrentUserId(),
            FullName = request.FullName,
            AvatarUrl = request.AvatarUrl
        };
        var result = await _mediator.Send(command);
        if (!result.Success) return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Upload avatar cho user (IFormFile → Azure Blob → lưu URL vào DB)</summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 400)]
    public async Task<IActionResult> UploadAvatar([FromForm] SingleFileUploadRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<UserProfileDto>.Fail("Vui lòng chọn file ảnh.", 400, "NO_FILE"));

        // Upload lên Azure Blob Storage
        using var stream = file.OpenReadStream();
        var imageUrl = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        // Lưu URL vào profile
        var command = new UpdateProfileCommand
        {
            UserId = GetCurrentUserId(),
            AvatarUrl = imageUrl
        };
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
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

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
}
