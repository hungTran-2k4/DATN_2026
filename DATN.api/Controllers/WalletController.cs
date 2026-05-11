using DATN.Application.Common.Models;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletRepository _walletRepo;

    public WalletController(IWalletRepository walletRepo)
    {
        _walletRepo = walletRepo;
    }

    [HttpGet("{shopId}/balance")]
    public async Task<ActionResult<ApiResponse<object>>> GetBalance(Guid shopId, CancellationToken ct)
    {
        var available = await _walletRepo.GetAvailableBalanceAsync(shopId, ct);
        var locked = await _walletRepo.GetLockedBalanceAsync(shopId, ct);

        return Ok(ApiResponse<object>.Succeed(new
        {
            AvailableBalance = available,
            LockedBalance = locked
        }));
    }

    [HttpGet("{shopId}/history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WalletLedger>>>> GetHistory(Guid shopId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var history = await _walletRepo.GetLedgersAsync(shopId, limit, ct);
        return Ok(ApiResponse<IEnumerable<WalletLedger>>.Succeed(history));
    }
}
