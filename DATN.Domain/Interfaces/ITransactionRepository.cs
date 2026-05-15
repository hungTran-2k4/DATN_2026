using DATN.Domain.Entities.Orders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(string? keyword, int pageNumber, int pageSize, CancellationToken ct = default);
}
