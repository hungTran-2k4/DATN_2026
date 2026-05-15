using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

/// <summary>
/// Repository cho bảng payments — dùng LLBLGen (đồng nhất với OrderRepository).
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly DataAccessAdapter _adapter;

    public PaymentRepository(DataAccessAdapter adapter) => _adapter = adapter;

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
        payment.CreatedAt = DateTime.UtcNow;

        var entity = new PaymentEntity
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Provider = payment.Provider,
            TransactionId = payment.TransactionId,
            Amount = payment.Amount,
            Status = payment.Status,
            ResponseCode = payment.ResponseCode,
            BankCode = payment.BankCode,
            CardType = payment.CardType,
            PayDate = payment.PayDate,
            RawResponse = payment.RawResponse,
            Signature = payment.Signature,
            Currency = payment.Currency,
            CreatedAt = payment.CreatedAt,
            IsNew = true
        };

        await _adapter.SaveEntityAsync(entity, cancellationToken: ct);
        return payment;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var col = new EntityCollection<PaymentEntity>();
        var sort = new SortExpression(PaymentFields.CreatedAt.Descending());

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = PaymentFields.OrderId == orderId,
            SorterToUse = sort,
            RowsToTake = 1
        }, ct);

        return col.FirstOrDefault() is { } e ? MapToDomain(e) : null;
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, string provider, CancellationToken ct = default)
    {
        var col = new EntityCollection<PaymentEntity>();
        IPredicateExpression filter = new PredicateExpression(PaymentFields.TransactionId == transactionId);
        filter.AddWithAnd(PaymentFields.Provider == provider);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, ct);

        return col.FirstOrDefault() is { } e ? MapToDomain(e) : null;
    }

    public async Task<bool> UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        var col = new EntityCollection<PaymentEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = PaymentFields.Id == payment.Id,
            RowsToTake = 1
        }, ct);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.TransactionId = payment.TransactionId;
        entity.Status = payment.Status;
        entity.ResponseCode = payment.ResponseCode;
        entity.BankCode = payment.BankCode;
        entity.CardType = payment.CardType;
        entity.PayDate = payment.PayDate;
        entity.RawResponse = payment.RawResponse;
        entity.Signature = payment.Signature;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsNew = false;

        return await _adapter.SaveEntityAsync(entity, cancellationToken: ct);
    }

    public async Task<IEnumerable<Payment>> GetByGroupKeyAsync(string groupKey, CancellationToken ct = default)
    {
        var col = new EntityCollection<PaymentEntity>();
        
        // Tìm tất cả payment records có RawResponse chứa groupKey (ví dụ: "grouped_with:{primaryOrderId}")
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = PaymentFields.RawResponse % $"%{groupKey}%"
        }, ct);

        return col.Select(MapToDomain).ToList();
    }

    private static Payment MapToDomain(PaymentEntity e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId,
        Provider = e.Provider,
        TransactionId = e.TransactionId,
        Amount = e.Amount,
        Status = e.Status,
        ResponseCode = e.ResponseCode,
        BankCode = e.BankCode,
        CardType = e.CardType,
        PayDate = e.PayDate,
        RawResponse = e.RawResponse,
        Signature = e.Signature,
        Currency = e.Currency,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
