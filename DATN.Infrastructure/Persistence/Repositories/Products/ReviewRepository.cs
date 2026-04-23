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

public class ReviewRepository : IReviewRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public ReviewRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Review> Items, int Total)> GetByProductIdAsync(Guid productId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Join Review → ProductVariant để lọc theo ProductId
        // LLBLGen: dùng RelationCollection + FilterToUse, không dùng QueryTarget trong scalar query
        var relations = new RelationCollection(ReviewEntity.Relations.ProductVariantEntityUsingVariantId);
        var filter = new PredicateExpression(ProductVariantFields.ProductId == productId);

        // Count: fetch toàn bộ ID để đếm (hoặc dùng EntityQuery count)
        var qf = new QueryFactory();
        var countQuery = qf.Review
            .Select(ReviewFields.Id.Count())
            .From(QueryTarget.InnerJoin(qf.ProductVariant)
                .On(ReviewFields.VariantId == ProductVariantFields.Id))
            .Where(filter);

        int totalCount;
        try
        {
            totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);
        }
        catch
        {
            // Fallback: fetch all IDs và đếm
            var countCol = new EntityCollection<ReviewEntity>();
            await _adapter.FetchEntityCollectionAsync(new QueryParameters
            {
                CollectionToFetch = countCol,
                FilterToUse = filter,
                RelationsToUse = relations,
            }, cancellationToken);
            totalCount = countCol.Count;
        }

        // Fetch paged
        var col = new EntityCollection<ReviewEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RelationsToUse = relations,
            SorterToUse = new SortExpression(ReviewFields.CreatedAt.Descending()),
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Review>>(col), totalCount);
    }

    public async Task<(IEnumerable<Review> Items, int Total)> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ReviewEntity>();
        var filter = new PredicateExpression(ReviewFields.UserId == userId);

        var qf = new QueryFactory();
        var totalCount = await _adapter.FetchScalarAsync<int>(qf.Create().Select(ReviewFields.Id.Count()).Where(filter), cancellationToken);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(ReviewFields.CreatedAt.Descending()),
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Review>>(col), totalCount);
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ReviewEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = ReviewFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Review>(entity);
    }

    public async Task<bool> HasUserReviewedAsync(Guid userId, Guid variantId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<ReviewEntity>();
        var filter = new PredicateExpression(ReviewFields.UserId == userId);
        filter.AddWithAnd(ReviewFields.VariantId == variantId);
        filter.AddWithAnd(ReviewFields.OrderId == orderId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);

        return col.Any();
    }

    public async Task<Review> CreateAsync(Review review, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ReviewEntity>(review);
        entity.IsNew = true;
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Review>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = new ReviewEntity(id);
        entity.IsNew = false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<(double AverageRating, int TotalReviews)> GetProductRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Dùng RelationCollection thay vì QueryTarget trong scalar query
        var relations = new RelationCollection(ReviewEntity.Relations.ProductVariantEntityUsingVariantId);
        var filter = new PredicateExpression(ProductVariantFields.ProductId == productId);

        var col = new EntityCollection<ReviewEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RelationsToUse = relations
        }, cancellationToken);

        var total = col.Count;
        if (total == 0) return (0.0, 0);

        var avg = col.Select(r => r.Rating).Average();
        return (Math.Round((double)avg, 1), total);
    }
}
