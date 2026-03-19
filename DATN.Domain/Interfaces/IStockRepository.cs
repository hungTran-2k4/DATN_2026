using DATN.Domain.Entities.Products;

namespace DATN.Domain.Interfaces;

public interface IStockRepository
{
    // Stock methods
    Task<Stock?> GetStockByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stock>> GetStocksByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStockAsync(Stock stock, CancellationToken cancellationToken = default);
    
    // High-level operations (reserving, committing, restocking)
    Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> CommitReservedStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RestockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    // StockTransaction methods
    Task<StockTransaction> AddTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetTransactionsByVariantAsync(Guid variantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockTransaction> Items, int TotalCount)> GetTransactionsByShopAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken = default);
}
