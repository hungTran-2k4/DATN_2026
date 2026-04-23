using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;

namespace DATN.Infrastructure.Persistence.Repositories.Products;

public class StockRepository : IStockRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public StockRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<Stock?> GetStockByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Stock
            .Where(StockFields.VariantId == variantId);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        return entity == null ? null : _mapper.Map<Stock>(entity);
    }

    public async Task<IEnumerable<Stock>> GetStocksByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
         var qf = new QueryFactory();

         // Fetch all variant stocks for a specific product
         var query = qf.Stock
             .From(QueryTarget.InnerJoin(qf.ProductVariant).On(StockFields.VariantId == ProductVariantFields.Id))
             .Where(ProductVariantFields.ProductId == productId);

         var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
         return _mapper.Map<IEnumerable<Stock>>(entities);
    }

    public async Task<bool> UpdateStockAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<StockEntity>(stock);
        entity.IsNew = false; // It's an update
        
        return await _adapter.SaveEntityAsync(entity, true, cancellationToken);
    }

    public async Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return false;
        
        var qf = new QueryFactory();
        var entity = await _adapter.FetchFirstAsync(qf.Stock.Where(StockFields.VariantId == variantId), cancellationToken);
        if (entity == null) return false;

        // Check availability
        if ((entity.PhysicalQuantity - entity.ReservedQuantity) < quantity) return false;

        // Update fields
        entity.ReservedQuantity += quantity;
        entity.UpdatedAt = DateTime.UtcNow;

        return await _adapter.SaveEntityAsync(entity, true, cancellationToken);
    }

    public async Task<bool> CommitReservedStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return false;

        var qf = new QueryFactory();
        var entity = await _adapter.FetchFirstAsync(qf.Stock.Where(StockFields.VariantId == variantId), cancellationToken);
        if (entity == null) return false;

        // Ensure we don't commit more than reserved
        if (entity.ReservedQuantity < quantity) return false;

        entity.PhysicalQuantity -= quantity;
        entity.ReservedQuantity -= quantity;
        entity.UpdatedAt = DateTime.UtcNow;

        return await _adapter.SaveEntityAsync(entity, true, cancellationToken);
    }

    public async Task<bool> RestockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return false;

        var qf = new QueryFactory();
        var entity = await _adapter.FetchFirstAsync(qf.Stock.Where(StockFields.VariantId == variantId), cancellationToken);
        
        if (entity == null)
        {
            // Initialize stock if it somehow didn't exist
            entity = new StockEntity();
            entity.VariantId = variantId;
            entity.PhysicalQuantity = quantity;
            entity.ReservedQuantity = 0;
            entity.IsNew = true;
        }
        else
        {
            entity.PhysicalQuantity += quantity;
        }
        
        entity.UpdatedAt = DateTime.UtcNow;

        return await _adapter.SaveEntityAsync(entity, true, cancellationToken);
    }

    public async Task<StockTransaction> AddTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<StockTransactionEntity>(transaction);
        
        if (entity.IsNew && entity.Id == Guid.Empty)
        {
             entity.Id = Guid.NewGuid();
        }
        
        entity.CreatedAt ??= DateTime.UtcNow;
        
        await _adapter.SaveEntityAsync(entity, true, cancellationToken);
        return _mapper.Map<StockTransaction>(entity);
    }

    public async Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetTransactionsByVariantAsync(Guid variantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();

        var query = qf.StockTransaction
            .Where(StockTransactionFields.VariantId == variantId)
            .OrderBy(StockTransactionFields.CreatedAt.Descending());

        var (total, elements) = await FetchPagedAsync(_adapter, query, page, pageSize, cancellationToken);
        return (_mapper.Map<IEnumerable<StockTransaction>>(elements), total);
    }

    public async Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetTransactionsByShopAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();

        var query = qf.StockTransaction
            .Where(StockTransactionFields.ShopId == shopId)
            .OrderBy(StockTransactionFields.CreatedAt.Descending());

        var (total, elements) = await FetchPagedAsync(_adapter, query, page, pageSize, cancellationToken);
        return (_mapper.Map<IEnumerable<StockTransaction>>(elements), total);
    }
    
    private async Task<(int TotalCount, List<StockTransactionEntity> Elements)> FetchPagedAsync(
        DataAccessAdapter adapter, EntityQuery<StockTransactionEntity> baseQuery, int page, int pageSize, CancellationToken cancellationToken)
    {
        var countQuery = baseQuery.Select(Functions.CountRow());
        var totalCount = await adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        if (totalCount == 0) return (0, new List<StockTransactionEntity>());

        var pagedQuery = baseQuery.Limit(pageSize).Offset((page - 1) * pageSize);
        var elements = await adapter.FetchQueryAsync(pagedQuery, cancellationToken);

        return (totalCount, elements.Cast<StockTransactionEntity>().ToList());
    }
}
