using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DATN.Application.Features.Users.Commands;
using DATN.Application.Features.Users.Queries;
using DATN.Application.DTOs.Auth;
using DATN.Application.DTOs.Users;
using DATN.Application.Common.Models;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Authorize(Roles = "Admin")] // Only Admin can access these endpoints
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ──────────────── LISTING & DETAIL ────────────────

    /// <summary>
    /// Lấy danh sách users (Admin Only, có phân trang, hỗ trợ Kendo filter)
    /// </summary>
    [HttpPost("paging")]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<IEnumerable<UserDto>>>> PagingUsers([FromBody] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết user theo ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(userId));
        if (!result.Success)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ──────────────── UPDATE ────────────────

    /// <summary>
    /// Admin cập nhật thông tin user (FullName)
    /// </summary>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserRequest request)
    {
        var command = new AdminUpdateUserCommand(userId, request.FullName);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User updated successfully" });
    }

    // ──────────────── LOCK / UNLOCK ────────────────

    /// <summary>
    /// Khóa tài khoản user (tạm thời, admin action)
    /// </summary>
    [HttpPatch("{userId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockUser(Guid userId, [FromBody] LockUserRequest? request)
    {
        var command = new LockUserCommand(userId, request?.Reason);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User locked successfully" });
    }

    /// <summary>
    /// Mở khóa tài khoản user
    /// </summary>
    [HttpPatch("{userId:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(Guid userId)
    {
        var command = new UnlockUserCommand(userId);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User unlocked successfully" });
    }

    // ──────────────── DEACTIVATE / ACTIVATE ────────────────

    /// <summary>
    /// Vô hiệu hóa tài khoản (dài hạn / soft delete)
    /// </summary>
    [HttpPatch("{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId, [FromBody] LockUserRequest? request)
    {
        var command = new DeactivateUserCommand(userId, request?.Reason);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User deactivated successfully" });
    }

    /// <summary>
    /// Kích hoạt lại tài khoản
    /// </summary>
    [HttpPatch("{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        var command = new ActivateUserCommand(userId);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User activated successfully" });
    }

    // ──────────────── ROLES ────────────────

    /// <summary>
    /// Gán Role cho User (Admin Only)
    /// </summary>
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(userId, request.RoleId);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User or Role not found" });
        return Ok(new { Message = "Role assigned successfully" });
    }

    /// <summary>
    /// Cập nhật danh sách Roles cho User (Thay thế toàn bộ)
    /// </summary>
    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] UpdateUserRolesRequest request)
    {
        var command = new UpdateUserRolesCommand(userId, request.RoleIds);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "User roles updated successfully" });
    }

    // ──────────────── RESET PASSWORD ────────────────

    /// <summary>
    /// Admin trigger reset password cho user (gửi email)
    /// </summary>
    [HttpPost("{userId:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword(Guid userId)
    {
        var command = new AdminResetPasswordCommand(userId);
        var success = await _mediator.Send(command);
        if (!success) return NotFound(new { Message = "User not found" });
        return Ok(new { Message = "Password reset email sent successfully" });
    }

    // ──────────────── AUDIT LOG ────────────────

    /// <summary>
    /// Xem lịch sử hoạt động của user
    /// </summary>
    [HttpGet("{userId:guid}/audit-logs")]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetUserAuditLogsQuery(userId, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Xem lịch sử đăng nhập của user
    /// </summary>
    [HttpGet("{userId:guid}/login-history")]
    [ProducesResponseType(typeof(PagedResponse<IEnumerable<LoginAttemptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoginHistory(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetUserLoginHistoryQuery(userId, page, pageSize));
        return Ok(result);
    }

    // ──────────────── SESSIONS ────────────────

    /// <summary>
    /// Xem danh sách sessions active của user
    /// </summary>
    [HttpGet("{userId:guid}/sessions")]
    [ProducesResponseType(typeof(IEnumerable<UserSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(Guid userId)
    {
        var result = await _mediator.Send(new GetUserSessionsQuery(userId));
        return Ok(result);
    }

    /// <summary>
    /// Kill 1 session cụ thể
    /// </summary>
    [HttpDelete("{userId:guid}/sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeSession(Guid userId, Guid sessionId)
    {
        await _mediator.Send(new RevokeUserSessionCommand(userId, sessionId));
        return Ok(new { Message = "Session revoked successfully" });
    }

    /// <summary>
    /// Kill tất cả sessions (force logout)
    /// </summary>
    [HttpDelete("{userId:guid}/sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions(Guid userId)
    {
        await _mediator.Send(new RevokeAllUserSessionsCommand(userId));
        return Ok(new { Message = "All sessions revoked successfully" });
    }
}

public class AssignRoleRequest
{
    public Guid RoleId { get; set; }
}

public class UpdateUserRolesRequest
{
    public List<Guid> RoleIds { get; set; } = new();
}
