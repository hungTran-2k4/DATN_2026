using AutoMapper;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
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

    public async Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(Guid? shopId = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        IPredicateExpression filter = new PredicateExpression();

        if (shopId.HasValue)
            filter.AddWithAnd(ProductFields.ShopId == shopId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression(ProductFields.Name % $"%{search}%");
            searchFilter.AddWithOr(ProductFields.Sku % $"%{search}%");
            filter.AddWithAnd(searchFilter);
        }

        var countQuery = qf.Create().Select(ProductFields.Id.Count()).Where(filter);
        var totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        var query = qf.Product.Where(filter)
                      .OrderBy(ProductFields.CreatedAt.Descending())
                      .Page(page, pageSize);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        var items = _mapper.Map<IEnumerable<Product>>(entities);
        
        return (items, totalCount);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Product.OrderBy(ProductFields.CreatedAt.Descending());
        
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
        var query = qf.Product.Where(ProductFields.Id == id);
        
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
        var query = qf.Product.Where(filter);

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
}
