using AutoMapper;
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

public class BrandRepository : IBrandRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public BrandRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Brand> Items, int Total)> GetPagedAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<BrandEntity>();
        var filter = new PredicateExpression();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression();
            searchFilter.AddWithOr(BrandFields.Name.UnaccentILike(search));
            searchFilter.AddWithOr(BrandFields.Slug.UnaccentILike(search));
            filter.Add(searchFilter);
        }

        var qf = new QueryFactory();
        var totalCount = await _adapter.FetchScalarAsync<int>(qf.Create().Select(BrandFields.Id.Count()).Where(filter), cancellationToken);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(BrandFields.Name.Ascending()),
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Brand>>(col), totalCount);
    }

    public async Task<IEnumerable<Brand>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<BrandEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = BrandFields.IsActive == true,
            SorterToUse = new SortExpression(BrandFields.Name.Ascending())
        }, cancellationToken);
        return _mapper.Map<IEnumerable<Brand>>(col);
    }

    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<BrandEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = BrandFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);
        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Brand>(entity);
    }

    public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<BrandEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = BrandFields.Slug == slug,
            RowsToTake = 1
        }, cancellationToken);
        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Brand>(entity);
    }

    public async Task<Brand> AddAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<BrandEntity>(brand);
        entity.IsNew = true;
        entity.Id = Guid.NewGuid();
        
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Brand>(entity);
    }

    public async Task<bool> UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<BrandEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = BrandFields.Id == brand.Id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        _mapper.Map(brand, entity);
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Chú ý: Nên kiểm tra xem Brand có Product nào không trước khi xóa.
        // Tuy nhiên hàm xóa cứng sẽ throw FK exception nếu có product, 
        // tùy chọn có thể catch hoặc set IsActive = false logic ở tầng handler.
        var entity = new BrandEntity(id);
        entity.IsNew = false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = new PredicateExpression(BrandFields.Slug == slug);
        if (excludeId.HasValue)
            filter.Add(BrandFields.Id != excludeId.Value);

        var qf = new QueryFactory();
        var q = qf.Create().Select(BrandFields.Id).Where(filter).Limit(1);
        
        var col = new EntityCollection<BrandEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);
        return col.Any();
    }
}
