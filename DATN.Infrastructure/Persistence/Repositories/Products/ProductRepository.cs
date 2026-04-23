using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using DATN.Domain.Common.Models;
using DATN.Infrastructure.Extensions;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using DATN_2026.DatabaseSpecific;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Persistence.Repositories.Products;

public class ProductRepository : IProductRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public ProductRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(Guid? shopId = null, string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        IPredicateExpression predicate = filter.ToPredicateExpression(new ProductEntity().Fields);

        if (shopId.HasValue)
            predicate.AddWithAnd(ProductFields.ShopId == shopId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression();
            searchFilter.AddWithOr(ProductFields.Name.UnaccentILike(search));
            searchFilter.AddWithOr(ProductFields.Description.UnaccentILike(search));
            searchFilter.AddWithOr(ProductFields.Slug.UnaccentILike(search));
            searchFilter.AddWithOr(ProductFields.Sku.UnaccentILike(search));
            predicate.AddWithAnd(searchFilter);
        }

        var countQuery = qf.Create().Select(ProductFields.Id.Count()).Where(predicate);
        var totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        var query = qf.Product.Where(predicate)
                      .OrderBy(ProductFields.CreatedAt.Descending())
                      .WithPath(ProductEntity.PrefetchPathProductImages)
                      .WithPath(ProductEntity.PrefetchPathProductVariants.WithSubPath(ProductVariantEntity.PrefetchPathStock))
                      .Page(page, pageSize);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        var items = _mapper.Map<IEnumerable<Product>>(entities);
        
        return (items, totalCount);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Product.OrderBy(ProductFields.CreatedAt.Descending())
                      .WithPath(ProductEntity.PrefetchPathProductImages)
                      .WithPath(ProductEntity.PrefetchPathProductVariants.WithSubPath(ProductVariantEntity.PrefetchPathStock));
        
        if (shopId.HasValue)
        {
            query.Where(ProductFields.ShopId == shopId.Value);
        }
        
        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        return _mapper.Map<IEnumerable<Product>>(entities);
    }

    public async Task<Product?> GetByIdAsync(Guid id, Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Product.Where(ProductFields.Id == id)
                      .WithPath(ProductEntity.PrefetchPathProductImages)
                      .WithPath(ProductEntity.PrefetchPathProductVariants.WithSubPath(ProductVariantEntity.PrefetchPathStock));
        
        if (shopId.HasValue)
        {
            query.Where(ProductFields.ShopId == shopId.Value);
        }
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return null;
        
        return _mapper.Map<Product>(entity);
    }

    public async Task<Product?> GetBySkuOrSlugAsync(string sku, string slug, Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var filter = new PredicateExpression(ProductFields.Sku == sku | ProductFields.Slug == slug);
        var query = qf.Product.Where(filter)
                      .WithPath(ProductEntity.PrefetchPathProductImages)
                      .WithPath(ProductEntity.PrefetchPathProductVariants.WithSubPath(ProductVariantEntity.PrefetchPathStock));

        if (shopId.HasValue)
        {
            query.Where(ProductFields.ShopId == shopId.Value);
        }
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return null;
        
        return _mapper.Map<Product>(entity);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ProductEntity>(product);
        entity.IsNew = true;
        
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Product>(entity);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Product.Where(ProductFields.Id == product.Id);
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return false;
        
        _mapper.Map(product, entity); // Map updated fields back to fetched entity
        entity.IsNew = false;
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = new ProductEntity(id);
        entity.IsNew = false;
        
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ProductImage>> GetImagesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductImageEntity>();
        var filter = new PredicateExpression(ProductImageFields.ProductId == productId);
        
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(ProductImageFields.DisplayOrder.Ascending())
        }, cancellationToken);

        return _mapper.Map<IEnumerable<ProductImage>>(col);
    }

    public async Task<ProductImage> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ProductImageEntity>(image);
        entity.IsNew = true;
        entity.Id = Guid.NewGuid();
        
        if (entity.DisplayOrder == null)
        {
             // Get max display order
             var qf = new QueryFactory();
             var maxQ = qf.Create().Select(ProductImageFields.DisplayOrder.Max()).Where(ProductImageFields.ProductId == image.ProductId);
             var maxVal = await _adapter.FetchScalarAsync<object>(maxQ, cancellationToken);
             entity.DisplayOrder = maxVal != DBNull.Value && maxVal != null ? Convert.ToInt32(maxVal) + 1 : 1;
        }

        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<ProductImage>(entity);
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var entity = new ProductImageEntity(imageId);
        entity.IsNew = false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> SetMainImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        // Set all to false first
        var updateToFalse = new ProductImageEntity();
        updateToFalse.IsPrimary = false;
        var filterFalse = new PredicateExpression(ProductImageFields.ProductId == productId);
        await _adapter.UpdateEntitiesDirectlyAsync(updateToFalse, new RelationPredicateBucket(filterFalse), cancellationToken);

        // Set target to true
        var updateToTrue = new ProductImageEntity();
        updateToTrue.IsPrimary = true;
        var filterTrue = new PredicateExpression(ProductImageFields.Id == imageId);
        await _adapter.UpdateEntitiesDirectlyAsync(updateToTrue, new RelationPredicateBucket(filterTrue), cancellationToken);

        return true;
    }
}
