using DATN.Application.Common.Models;
using DATN.Application.DTOs.Marketing;
using DATN.Application.Features.Vouchers.Commands;
using DATN.Application.Features.Vouchers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VouchersController : ControllerBase
{
    private readonly IMediator _mediator;

    public VouchersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // --- Admin / Shop Endpoints --- //

    [HttpGet]
    public async Task<ActionResult<PagedResponse<IEnumerable<VoucherDto>>>> GetPaged(
        [FromQuery] string? search, [FromQuery] Guid? shopId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetVouchersQuery(search, shopId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetActive([FromQuery] Guid? shopId)
    {
        var result = await _mediator.Send(new GetActiveVouchersQuery(shopId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetVoucherByIdQuery(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> GetByCode(string code, [FromQuery] Guid? shopId)
    {
        var result = await _mediator.Send(new GetVoucherByCodeQuery(code, shopId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Create([FromBody] CreateVoucherCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Update(Guid id, [FromBody] UpdateVoucherCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse<VoucherDto>.Fail("ID mismatch in URL and body"));
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteVoucherCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // --- User Vouchers Endpoints --- //

    [HttpGet("my-vouchers")]
    // [Authorize] - Uncomment when auth is fully integrated
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetMyVouchers([FromQuery] bool isUsed = false)
    {
        // Replace with actual UserId fetch
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var result = await _mediator.Send(new GetUserSavedVouchersQuery(userId, isUsed));
        return Ok(result);
    }

    [HttpPost("{id:guid}/save")]
    // [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> SaveVoucher(Guid id)
    {
        var result = await _mediator.Send(new SaveVoucherCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/apply")]
    // [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ApplyVoucher(Guid id)
    {
        var result = await _mediator.Send(new ApplyVoucherCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
