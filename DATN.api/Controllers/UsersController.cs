using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Features.Users.Commands;
using MyProject.Application.Features.Users.Queries;
using MyProject.Application.Models.Auth;

namespace MyProject.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Only Admin can access these endpoints
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách tất cả users (Admin Only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var query = new GetUsersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gán Role cho User (Admin Only)
    /// </summary>
    [HttpPost("{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(userId, request.RoleId);
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound("User or Role not found");
        }

        return Ok(new { Message = "Role assigned successfully" });
    }

    /// <summary>
    /// Cập nhật danh sách Roles cho User (Thay thế toàn bộ)
    /// </summary>
    [HttpPut("{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] UpdateUserRolesRequest request)
    {
        var command = new UpdateUserRolesCommand(userId, request.RoleIds);
        var success = await _mediator.Send(command);

        if (!success)
        {
            return NotFound("User not found");
        }

        return Ok(new { Message = "User roles updated successfully" });
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
