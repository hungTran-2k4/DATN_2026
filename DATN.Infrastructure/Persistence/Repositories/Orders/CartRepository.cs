using DATN.Domain.Entities.Orders;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace DATN.Infrastructure.Persistence.Repositories.Orders;

public class CartRepository : ICartRepository
{
    private readonly DataAccessAdapter _adapter;

    public CartRepository(DataAccessAdapter adapter) => _adapter = adapter;

    public async Task<IEnumerable<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        // Prefetch: Cart → ProductVariant → Product (for shop/name), Stock (for qty)
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.CartEntity);
        var variantPath = prefetch.Add(CartEntity.PrefetchPathProductVariant);
        var productPath = variantPath.SubPath.Add(ProductVariantEntity.PrefetchPathProduct);
        productPath.SubPath.Add(ProductEntity.PrefetchPathShop); // Fetch Shop info
        productPath.SubPath.Add(ProductEntity.PrefetchPathProductImages); // Fetch Product Images for fallback
        variantPath.SubPath.Add(ProductVariantEntity.PrefetchPathStock);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.UserId == userId,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        return col.Select(e =>
        {
            var variant = e.ProductVariant;
            var product = variant?.Product;
            var shop = product?.Shop;

            // Fallback: Variant Image -> Primary Product Image -> First Product Image
            var imageUrl = variant?.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl) && product != null)
            {
                var primaryImage = product.ProductImages.FirstOrDefault(img => img.IsPrimary == true);
                imageUrl = primaryImage?.Url ?? product.ProductImages.FirstOrDefault()?.Url;
            }

            return new CartItem
            {
                Id = e.Id,
                UserId = e.UserId,
                VariantId = e.VariantId,
                Quantity = e.Quantity,
                CreatedAt = e.CreatedAt,
                ShopId = product?.ShopId,
                ShopName = shop?.Name,
                ProductName = product?.Name,
                VariantName = variant?.Name,
                VariantImageUrl = imageUrl,
                UnitPrice = variant?.Price ?? 0m,
                VariantAttributes = variant?.VariantAttributes,
                StockQty = (int)(variant?.Stock?.AvailableQuantity ?? 0)
            };
        }).ToList();
    }

    public async Task<CartItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.Id == id & CartFields.UserId == userId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : new CartItem
        {
            Id = entity.Id,
            UserId = entity.UserId,
            VariantId = entity.VariantId,
            Quantity = entity.Quantity,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<CartItem?> GetByVariantIdAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.UserId == userId & CartFields.VariantId == variantId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : new CartItem
        {
            Id = entity.Id,
            UserId = entity.UserId,
            VariantId = entity.VariantId,
            Quantity = entity.Quantity,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<CartItem> AddAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        var entity = new CartEntity
        {
            Id = item.Id,
            UserId = item.UserId,
            VariantId = item.VariantId,
            Quantity = item.Quantity,
            CreatedAt = item.CreatedAt,
            IsNew = true
        };
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        item.Id = entity.Id;
        return item;
    }

    public async Task<bool> UpdateQuantityAsync(Guid id, Guid userId, int quantity, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.Id == id & CartFields.UserId == userId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.Quantity = quantity;
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.Id == id & CartFields.UserId == userId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CartEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CartFields.UserId == userId
        }, cancellationToken);

        if (!col.Any()) return true;
        return await _adapter.DeleteEntityCollectionAsync(col, cancellationToken) > 0;
    }

    public async Task<bool> RemoveByVariantIdsAsync(Guid userId, IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        var ids = variantIds.ToList();
        if (!ids.Any()) return true;

        var col = new EntityCollection<CartEntity>();
        // Build OR filter for multiple variantIds
        IPredicateExpression? filter = null;
        foreach (var vid in ids)
        {
            var pred = new PredicateExpression(CartFields.UserId == userId & CartFields.VariantId == vid);
            filter = filter == null
                ? pred
                : new PredicateExpression(filter) { pred };
        }

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter
        }, cancellationToken);

        if (!col.Any()) return true;
        return await _adapter.DeleteEntityCollectionAsync(col, cancellationToken) > 0;
    }
}
