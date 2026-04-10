using AutoMapper;
using DATN.Domain.Entities.Shops;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using DATN.Infrastructure.Extensions;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Persistence.Repositories.Shops;

public class ShopRepository : IShopRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepo;

    public ShopRepository(DataAccessAdapter adapter, IMapper mapper, IUserRepository userRepo)
    {
        _adapter = adapter;
        _mapper = mapper;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<Shop>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.OrderBy(ShopFields.CreatedAt.Descending());
        
        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        return _mapper.Map<IEnumerable<Shop>>(entities);
    }

    public async Task<IEnumerable<Shop>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Where(ShopFields.OwnerId == ownerId).OrderBy(ShopFields.CreatedAt.Descending());
        
        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        return _mapper.Map<IEnumerable<Shop>>(entities);
    }

    public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Where(ShopFields.Id == id);
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return null;
        
        return _mapper.Map<Shop>(entity);
    }

    public async Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Where(ShopFields.Slug == slug);
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return null;
        
        return _mapper.Map<Shop>(entity);
    }

    public async Task<Shop> AddAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ShopEntity>(shop);
        entity.IsNew = true;
        
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Shop>(entity);
    }

    public async Task<bool> UpdateAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Shop.Where(ShopFields.Id == shop.Id);
        
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return false;
        
        _mapper.Map(shop, entity); // Ghi đè các property từ domain sang entity
        entity.IsNew = false;
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = new ShopEntity(id);
        entity.IsNew = false;
        
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<(IEnumerable<Shop> Items, int TotalCount)> GetPagedAsync(string? search = null, DATN.Domain.Common.Models.FilterDescriptor? filter = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();

        // RẤT TỐT: Bạn đã Eager Load bảng User ở đây!
        var query = qf.Shop.WithPath(ShopEntity.PrefetchPathUser);
        var countQuery = qf.Shop.Select(Functions.CountRow());
        var predicate = new PredicateExpression();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search}%";
            predicate.AddWithAnd(ShopFields.Name.Like(searchPattern)
                .Or(ShopFields.Slug.Like(searchPattern)));
        }

        if (filter != null)
        {
            var kendoExpression = filter.ToPredicateExpression(new ShopEntity().Fields);
            if (kendoExpression != null)
            {
                predicate.AddWithAnd(kendoExpression);
            }
        }

        if (predicate.Count > 0)
        {
            query = query.Where(predicate);
            countQuery = countQuery.Where(predicate);
        }

        int totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        var pagedQuery = query.OrderBy(ShopFields.CreatedAt.Descending())
                              .Offset((page - 1) * pageSize)
                              .Limit(pageSize);

        var entities = await _adapter.FetchQueryAsync<ShopEntity>(pagedQuery, cancellationToken);
        var shopVM = _mapper.Map<IEnumerable<Shop>>(entities);
        

        // Trả về danh sách đã được map và tổng số lượng
        return (shopVM, totalCount);
    }
}
