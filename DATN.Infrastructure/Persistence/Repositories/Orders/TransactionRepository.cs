using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

public class TransactionRepository : ITransactionRepository
{
    private readonly DataAccessAdapter _adapter;

    public TransactionRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken ct = default)
    {
        if (transaction.Id == Guid.Empty) transaction.Id = Guid.NewGuid();
        if (transaction.CreatedAt == default) transaction.CreatedAt = DateTime.UtcNow;

        var entity = new TransactionEntity
        {
            Id = transaction.Id,
            OrderId = transaction.OrderId,
            ExternalTransactionNo = transaction.ExternalTransactionNo,
            Amount = transaction.Amount,
            Provider = transaction.Provider,
            Status = transaction.Status,
            RawResponse = transaction.RawResponse,
            CreatedAt = transaction.CreatedAt,
            TransactionType = transaction.TransactionType,
            ReferenceId = transaction.ReferenceId,
            IsNew = true
        };

        await _adapter.SaveEntityAsync(entity, cancellationToken: ct);
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var col = new EntityCollection<TransactionEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = TransactionFields.Id == id,
            RowsToTake = 1
        }, ct);
        
        var entity = col.FirstOrDefault();
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<IEnumerable<Transaction>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var col = new EntityCollection<TransactionEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = TransactionFields.OrderId == orderId
        }, ct);
        
        return col.Select(MapToDomain);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var col = new EntityCollection<TransactionEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = TransactionFields.Id == id,
            RowsToTake = 1
        }, ct);
        
        var entity = col.FirstOrDefault();
        if (entity != null)
        {
            entity.Status = status;
            entity.IsNew = false;
            return await _adapter.SaveEntityAsync(entity, cancellationToken: ct);
        }
        return false;
    }

    private static Transaction MapToDomain(TransactionEntity e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId ?? Guid.Empty,
        ExternalTransactionNo = e.ExternalTransactionNo,
        Amount = e.Amount,
        Provider = e.Provider,
        Status = e.Status,
        RawResponse = e.RawResponse,
        CreatedAt = e.CreatedAt ?? DateTime.UtcNow,
        TransactionType = e.TransactionType,
        ReferenceId = e.ReferenceId
    };
}
