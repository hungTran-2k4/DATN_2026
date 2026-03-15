using AutoMapper;
using DATN.Domain.Entities.Categories;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;

namespace DATN.Infrastructure.Persistence.Repositories.Categories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public CategoryRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Category> Items, int Total)> GetPagedAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        IPredicateExpression filter = new PredicateExpression();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression(CategoryFields.Name % $"%{search}%");
            searchFilter.AddWithOr(CategoryFields.Slug % $"%{search}%");
            filter.AddWithAnd(searchFilter);
        }

        var qf = new QueryFactory();
        var countQuery = qf.Create().Select(CategoryFields.Id.Count()).Where(filter);
        var totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(CategoryFields.DisplayOrder.Ascending()),
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Category>>(col), totalCount);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            SorterToUse = new SortExpression(CategoryFields.DisplayOrder.Ascending())
        }, cancellationToken);
        return _mapper.Map<IEnumerable<Category>>(col);
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CategoryFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);
        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Category>(entity);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CategoryFields.Slug == slug,
            RowsToTake = 1
        }, cancellationToken);
        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Category>(entity);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        IPredicateExpression filter = new PredicateExpression(CategoryFields.Slug == slug);
        if (excludeId.HasValue)
            filter = new PredicateExpression(filter) { CategoryFields.Id != excludeId.Value };

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);
        return col.Any();
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<CategoryEntity>(category);
        entity.IsNew = true;
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Category>(entity);
    }

    public async Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CategoryFields.Id == category.Id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        _mapper.Map(category, entity);
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<CategoryEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = CategoryFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.IsActive = false;
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }
}
