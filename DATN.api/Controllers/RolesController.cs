using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DATN.Application.DTOs.Auth;
using DATN.Application.Features.Roles.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ Roles trong hệ thống
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return Ok(result);
    }
}
