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

namespace DATN.Infrastructure.Persistence.Repositories.Products;

public class WishlistRepository : IWishlistRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public WishlistRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Product> Items, int Total)> GetProductsByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ProductEntity>();
        
        var qf = new QueryFactory();
        var filter = new PredicateExpression(WishlistFields.UserId == userId);
        var q = qf.Product.From(QueryTarget.InnerJoin(qf.Wishlist).On(ProductFields.Id == WishlistFields.ProductId))
                          .Where(filter);

        var totalCount = await _adapter.FetchScalarAsync<int>(qf.Create().Select(qf.Product.Select(ProductFields.Id).Count()).From(QueryTarget.InnerJoin(qf.Wishlist).On(ProductFields.Id == WishlistFields.ProductId)).Where(filter), cancellationToken);

        // Use Relations instead of RelationCollectionToUse for QueryParameters
        var relations = new RelationCollection(ProductEntity.Relations.WishlistEntityUsingProductId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RelationsToUse = relations,
            SorterToUse = new SortExpression(WishlistFields.CreatedAt.Descending()), // Sort by newest added
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Product>>(col), totalCount);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<WishlistEntity>();
        var filter = new PredicateExpression(WishlistFields.UserId == userId);
        filter.AddWithAnd(WishlistFields.ProductId == productId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);
        
        return col.Any();
    }

    public async Task<bool> AddAsync(WishlistItem item, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<WishlistEntity>(item);
        entity.IsNew = true;
        entity.CreatedAt = DateTime.UtcNow;
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var entity = new WishlistEntity(productId, userId)
        {
            IsNew = false
        };
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<int> GetProductWishlistCountAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var filter = new PredicateExpression(WishlistFields.ProductId == productId);
        var q = qf.Create().Select(WishlistFields.ProductId.Count()).Where(filter);
        return await _adapter.FetchScalarAsync<int>(q, cancellationToken);
    }
}
