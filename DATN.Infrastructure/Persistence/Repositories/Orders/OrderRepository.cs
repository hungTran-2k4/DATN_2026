using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

public class OrderRepository : IOrderRepository
{
    private readonly DataAccessAdapter _adapter;

    public OrderRepository(DataAccessAdapter adapter) => _adapter = adapter;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<OrderEntity>();
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.OrderEntity);
        prefetch.Add(OrderEntity.PrefetchPathOrderItems);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = OrderFields.Id == id,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        return col.FirstOrDefault() is { } e ? MapToOrder(e) : null;
    }

    public async Task<Order?> GetByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<OrderEntity>();
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.OrderEntity);
        prefetch.Add(OrderEntity.PrefetchPathOrderItems);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = OrderFields.OrderCode == orderCode,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        return col.FirstOrDefault() is { } e ? MapToOrder(e) : null;
    }

    public async Task<(IEnumerable<Order> Items, int Total)> GetByBuyerIdAsync(
        Guid buyerId, string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<OrderEntity>();

        IPredicateExpression filter = new PredicateExpression(OrderFields.BuyerId == buyerId);
        if (!string.IsNullOrEmpty(status))
            filter.AddWithAnd(OrderFields.OrderStatus == status);

        var qf = new QueryFactory();
        var countQuery = qf.Create().Select(OrderFields.Id.Count()).Where(filter);
        var totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        var sortClause = new SortExpression(OrderFields.CreatedAt.Descending());
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = sortClause,
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (col.Select(MapToOrder).ToList(), totalCount);
    }

    public async Task<(IEnumerable<Order> Items, int Total)> GetByShopIdAsync(
        Guid shopId, string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Order table doesn't have ShopId directly.
        // Determine shop's orders through join:
        // Order -> OrderItem (variant_id) -> ProductVariant -> Product (shop_id)

        var qf = new QueryFactory();

        IPredicateExpression filter = new PredicateExpression(ProductFields.ShopId == shopId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            filter.AddWithAnd(OrderFields.OrderStatus == status);
        }

        var orderJoin = qf.Order
            .InnerJoin(qf.OrderItem).On(OrderFields.Id == OrderItemFields.OrderId)
            .InnerJoin(qf.ProductVariant).On(OrderItemFields.VariantId == ProductVariantFields.Id)
            .InnerJoin(qf.Product).On(ProductVariantFields.ProductId == ProductFields.Id);

        // 1) Get distinct OrderIds for this shop (for paging + count)
        var idQuery = qf.Create()
            .From(orderJoin)
            .Select(OrderFields.Id)
            .Distinct()
            .OrderBy(OrderFields.CreatedAt.Descending())
            .Page(page, pageSize);

        var idRows = await _adapter.FetchQueryAsync(idQuery, cancellationToken);
        var ids = idRows
            .Cast<object[]>()
            .Select(r => (Guid)r[0])
            .ToList();

        // Count (MVP): fetch all distinct ids and count them.
        // For large scale, switch to COUNT(DISTINCT ...) at DB-level.
        var countIdsQuery = qf.Create()
            .From(orderJoin)
            .Select(OrderFields.Id)
            .Distinct();
        var allIdRows = await _adapter.FetchQueryAsync(countIdsQuery, cancellationToken);
        var totalCount = allIdRows.Count;

        if (!ids.Any())
        {
            return (Enumerable.Empty<Order>(), totalCount);
        }

        // 2) Fetch orders + items by ids
        var col = new EntityCollection<OrderEntity>();
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.OrderEntity);
        prefetch.Add(OrderEntity.PrefetchPathOrderItems);

        // Build OR predicate for ids
        IPredicateExpression filter2 = new PredicateExpression();
        foreach (var id in ids)
        {
            filter2.AddWithOr(OrderFields.Id == id);
        }

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter2,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        // Preserve paging order (CreatedAt desc)
        var mapped = col.Select(MapToOrder).ToDictionary(o => o.Id, o => o);
        var ordered = ids.Where(mapped.ContainsKey).Select(id => mapped[id]).ToList();

        return (ordered, totalCount);
    }

    public async Task<IEnumerable<Order>> CreateBulkAsync(IEnumerable<Order> orders, CancellationToken cancellationToken = default)
    {
        var result = new List<Order>();

        foreach (var order in orders)
        {
            order.OrderCode = await GenerateOrderCodeAsync(cancellationToken);

            var orderEntity = new OrderEntity
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                BuyerId = order.BuyerId,
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                CustomerNote = order.CustomerNote,
                CreatedAt = order.CreatedAt ?? DateTime.UtcNow,
                // OrderEntity does not have UpdatedAt
                IsNew = true
            };
            await _adapter.SaveEntityAsync(orderEntity, cancellationToken: cancellationToken);

            foreach (var item in order.Items)
            {
                item.OrderId = order.Id;
                var itemEntity = new OrderItemEntity
                {
                    Id = item.Id,
                    OrderId = order.Id,
                    VariantId = item.VariantId,
                    ProductNameSnapshot = item.ProductNameSnapshot,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    IsNew = true
                };
                await _adapter.SaveEntityAsync(itemEntity, cancellationToken: cancellationToken);
            }

            result.Add(order);
        }

        return result;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<OrderEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = OrderFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.OrderStatus = newStatus;
        // OrderEntity does not have UpdatedAt
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken = default)
    {
        string code;
        do
        {
            var random = new Random().Next(1000, 9999);
            code = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{random}";
            var existing = await GetByOrderCodeAsync(code, cancellationToken);
            if (existing == null) break;
        } while (true);

        return code;
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid id, string newPaymentStatus, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<OrderEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = OrderFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.PaymentStatus = newPaymentStatus;
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    private static Order MapToOrder(OrderEntity e) => new()
    {
        Id = e.Id,
        OrderCode = e.OrderCode ?? string.Empty,
        BuyerId = e.BuyerId ?? Guid.Empty,
        ShippingAddress = e.ShippingAddress ?? string.Empty,
        PaymentMethod = e.PaymentMethod,
        PaymentStatus = e.PaymentStatus,
        OrderStatus = e.OrderStatus,
        ShippingFee = e.ShippingFee ?? 0m,
        TotalAmount = e.TotalAmount,
        CustomerNote = e.CustomerNote,
        CreatedAt = e.CreatedAt,
        // UpdateAt does not exist on OrderEntity
        Items = e.OrderItems.Select(i => new OrderItem
        {
            Id = i.Id,
            OrderId = i.OrderId ?? Guid.Empty,
            VariantId = i.VariantId ?? Guid.Empty,
            ProductNameSnapshot = i.ProductNameSnapshot,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
