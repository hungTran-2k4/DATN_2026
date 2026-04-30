using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using DATN.Infrastructure.Extensions;

namespace DATN.Infrastructure.Persistence.Repositories.Products;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IUnitOfWork _uow;

    public ProductVariantRepository(DataAccessAdapter adapter, IUnitOfWork uow)
    {
        _adapter = adapter;
        _uow = uow;
    }

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductVariantEntity>();
        // Prefetch Stock để lấy available_quantity
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.ProductVariantEntity);
        prefetch.Add(ProductVariantEntity.PrefetchPathStock);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = ProductVariantFields.ProductId == productId,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        return col.Select(MapToVariant).ToList();
    }

    public async Task<(IEnumerable<ProductVariant> Items, int Total)> GetPagedAsync(
        Guid? productId, string? search, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductVariantEntity>();
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.ProductVariantEntity);
        prefetch.Add(ProductVariantEntity.PrefetchPathStock);

        IPredicateExpression filter = new PredicateExpression();

        if (productId.HasValue)
        {
            filter.AddWithAnd(ProductVariantFields.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression();
            searchFilter.AddWithOr(ProductVariantFields.Name.UnaccentILike(search));
            searchFilter.AddWithOr(ProductVariantFields.Sku.UnaccentILike(search));
            filter.AddWithAnd(searchFilter);
        }

        var qf = new QueryFactory();
        var countQuery = qf.Create().Select(ProductVariantFields.Id.Count()).Where(filter);
        var totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            PrefetchPathToUse = prefetch,
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (col.Select(MapToVariant).ToList(), totalCount);
    }

    public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductVariantEntity>();
        var prefetch = new PrefetchPath2((int)DATN_2026.EntityType.ProductVariantEntity);
        prefetch.Add(ProductVariantEntity.PrefetchPathStock);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = ProductVariantFields.Id == id,
            PrefetchPathToUse = prefetch
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : MapToVariant(entity);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductVariantEntity>();
        IPredicateExpression filter = new PredicateExpression(ProductVariantFields.Sku == sku);
        if (excludeId.HasValue)
            filter = new PredicateExpression(filter) { ProductVariantFields.Id != excludeId.Value };

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);

        return col.Any();
    }

    public async Task<ProductVariant> AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        // Wrap cả 2 bước trong transaction — nếu lưu stock lỗi thì rollback variant
        using var tx = _uow.BeginTransaction();
        try
        {
            // 1. Create variant entity
            var variantEntity = new ProductVariantEntity
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Name = variant.Name,
                Sku = variant.Sku,
                Price = variant.Price,
                Originalprice = variant.OriginalPrice,
                ImageUrl = variant.ImageUrl,
                VariantAttributes = variant.VariantAttributes,
                IsNew = true
            };
            await _adapter.SaveEntityAsync(variantEntity, refetchAfterSave: true, cancellationToken: cancellationToken);

            // 2. Create stock record
            // available_quantity là GENERATED COLUMN (physical_quantity - reserved_quantity) — không insert trực tiếp
            var stockEntity = new StockEntity
            {
                VariantId = variantEntity.Id,
                PhysicalQuantity = variant.StockQty,
                ReservedQuantity = 0,
                IsNew = true
            };
            await _adapter.SaveEntityAsync(stockEntity, cancellationToken: cancellationToken);

            tx.Commit();
            variant.Id = variantEntity.Id;
            return variant;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductVariantEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = ProductVariantFields.Id == variant.Id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.Name = variant.Name;
        entity.Sku = variant.Sku;
        entity.Price = variant.Price;
        entity.Originalprice = variant.OriginalPrice;
        entity.ImageUrl = variant.ImageUrl;
        entity.VariantAttributes = variant.VariantAttributes;
        entity.IsNew = false;

        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> BulkSaveAsync(IEnumerable<ProductVariant> creates, IEnumerable<ProductVariant> updates, CancellationToken cancellationToken = default)
    {
        using var tx = _uow.BeginTransaction();
        try
        {
            // 1. Process Updates
            foreach (var variant in updates)
            {
                var col = new EntityCollection<ProductVariantEntity>();
                await _adapter.FetchEntityCollectionAsync(new QueryParameters
                {
                    CollectionToFetch = col,
                    FilterToUse = ProductVariantFields.Id == variant.Id,
                    RowsToTake = 1
                }, cancellationToken);

                var entity = col.FirstOrDefault();
                if (entity != null)
                {
                    entity.Name = variant.Name;
                    entity.Sku = variant.Sku;
                    entity.Price = variant.Price;
                    entity.Originalprice = variant.OriginalPrice;
                    entity.ImageUrl = variant.ImageUrl;
                    entity.VariantAttributes = variant.VariantAttributes;
                    entity.IsNew = false;
                    await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
                }
            }

            // 2. Process Creates
            foreach (var variant in creates)
            {
                var variantEntity = new ProductVariantEntity
                {
                    Id = variant.Id,
                    ProductId = variant.ProductId,
                    Name = variant.Name,
                    Sku = variant.Sku,
                    Price = variant.Price,
                    Originalprice = variant.OriginalPrice,
                    ImageUrl = variant.ImageUrl,
                    VariantAttributes = variant.VariantAttributes,
                    IsNew = true
                };
                await _adapter.SaveEntityAsync(variantEntity, refetchAfterSave: true, cancellationToken: cancellationToken);

                var stockEntity = new StockEntity
                {
                    VariantId = variantEntity.Id,
                    PhysicalQuantity = variant.StockQty,
                    ReservedQuantity = 0,
                    IsNew = true
                };
                await _adapter.SaveEntityAsync(stockEntity, cancellationToken: cancellationToken);
                variant.Id = variantEntity.Id;
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = new ProductVariantEntity(id) { IsNew = false };
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<int> GetStockQtyAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        // Stock PK is VariantId
        var stockCol = new EntityCollection<StockEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = stockCol,
            FilterToUse = StockFields.VariantId == variantId,
            RowsToTake = 1
        }, cancellationToken);

        var stock = stockCol.FirstOrDefault();
        return stock?.AvailableQuantity ?? 0;
    }

    public async Task<bool> DeductStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var stockCol = new EntityCollection<StockEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = stockCol,
            FilterToUse = StockFields.VariantId == variantId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = stockCol.FirstOrDefault();
        if (entity == null || (entity.AvailableQuantity ?? 0) < quantity) return false;

        entity.PhysicalQuantity = entity.PhysicalQuantity - quantity;
        // Không set AvailableQuantity trực tiếp — là GENERATED COLUMN (physical - reserved)
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    private static ProductVariant MapToVariant(ProductVariantEntity e) => new()
    {
        Id = e.Id,
        ProductId = e.ProductId,
        Name = e.Name,
        Sku = e.Sku,
        Price = e.Price,
        OriginalPrice = e.Originalprice,
        ImageUrl = e.ImageUrl,
        VariantAttributes = e.VariantAttributes,
        // Stock được prefetch: AvailableQuantity là số thực tế để bán
        StockQty = e.Stock?.AvailableQuantity ?? 0
    };
}
