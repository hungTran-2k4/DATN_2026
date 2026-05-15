using DATN.Application.Common.Models;
using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepo;

    public TransactionsController(ITransactionRepository transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<IEnumerable<Transaction>>>> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _transactionRepo.GetPagedAsync(keyword, page, pageSize, ct);
        
        var result = PagedResponse<IEnumerable<Transaction>>.SucceedDefault(items, page, pageSize, totalCount);

        return Ok(result);
    }
}
