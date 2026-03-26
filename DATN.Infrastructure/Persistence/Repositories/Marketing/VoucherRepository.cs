using AutoMapper;
using DATN.Domain.Entities.Marketing;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using DATN.Infrastructure.Extensions;

namespace DATN.Infrastructure.Persistence.Repositories.Marketing;

public class VoucherRepository : IVoucherRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public VoucherRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<Voucher> Items, int Total)> GetPagedAsync(
        string? search = null, 
        Guid? shopId = null,
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        var filter = new PredicateExpression();

        if (shopId.HasValue)
        {
            filter.Add(VoucherFields.ShopId == shopId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = new PredicateExpression();
            searchFilter.AddWithOr(VoucherFields.Name.UnaccentILike(search));
            searchFilter.AddWithOr(VoucherFields.Code.UnaccentILike(search));
            filter.AddWithAnd(searchFilter);
        }

        var qf = new QueryFactory();
        var totalCount = await _adapter.FetchScalarAsync<int>(
            qf.Create().Select(VoucherFields.Id.Count()).Where(filter), 
            cancellationToken);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(VoucherFields.StartDate.Descending()),
            RowsToSkip = (page - 1) * pageSize,
            RowsToTake = pageSize
        }, cancellationToken);

        return (_mapper.Map<IEnumerable<Voucher>>(col), totalCount);
    }

    public async Task<IEnumerable<Voucher>> GetActiveVouchersAsync(Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        
        var filter = new PredicateExpression(VoucherFields.IsActive == true);
        filter.AddWithAnd(VoucherFields.StartDate <= DateTime.UtcNow);
        filter.AddWithAnd(VoucherFields.EndDate >= DateTime.UtcNow);
        
        // Custom logic: Where UsedCount < UsageLimit or UsageLimit = 0 (if 0 means unlimited, though strictly speaking its usage_limit>used_count)
        filter.AddWithAnd(VoucherFields.UsageLimit > VoucherFields.UsedCount);

        if (shopId.HasValue)
        {
            var shopFilter = new PredicateExpression(VoucherFields.ShopId == shopId.Value);
            shopFilter.AddWithOr(VoucherFields.ShopId.IsNull()); // Include platform vouchers
            filter.AddWithAnd(shopFilter);
        }
        else
        {
            filter.AddWithAnd(VoucherFields.ShopId.IsNull()); // Only platform vouchers
        }

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            SorterToUse = new SortExpression(VoucherFields.EndDate.Ascending())
        }, cancellationToken);

        return _mapper.Map<IEnumerable<Voucher>>(col);
    }

    public async Task<Voucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = VoucherFields.Id == id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Voucher>(entity);
    }

    public async Task<Voucher?> GetByCodeAsync(string code, Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        var filter = new PredicateExpression(VoucherFields.Code == code.ToUpper());

        if (shopId.HasValue)
        {
            var shopFilter = new PredicateExpression(VoucherFields.ShopId == shopId.Value);
            shopFilter.AddWithOr(VoucherFields.ShopId.IsNull());
            filter.AddWithAnd(shopFilter);
        }
        else
        {
            filter.AddWithAnd(VoucherFields.ShopId.IsNull());
        }

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<Voucher>(entity);
    }

    public async Task<Voucher> AddAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<VoucherEntity>(voucher);
        entity.IsNew = true;
        entity.Id = Guid.NewGuid();
        entity.UsedCount = 0;
        entity.Code = voucher.Code.ToUpper(); // Ensure upper case codes
        
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<Voucher>(entity);
    }

    public async Task<bool> UpdateAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = VoucherFields.Id == voucher.Id,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        _mapper.Map(voucher, entity);
        entity.IsNew = false;
        entity.Code = voucher.Code.ToUpper();
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = new VoucherEntity(id);
        entity.IsNew = false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    // User Voucher Interactions
    public async Task<bool> SaveVoucherForUserAsync(UserVoucher userVoucher, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<UserVoucherEntity>(userVoucher);
        entity.IsNew = true;
        entity.SavedAt = DateTime.UtcNow;
        entity.IsUsed = false;
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> HasUserSavedVoucherAsync(Guid userId, Guid voucherId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserVoucherEntity>();
        var filter = new PredicateExpression(UserVoucherFields.UserId == userId);
        filter.AddWithAnd(UserVoucherFields.VoucherId == voucherId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);

        return col.Any();
    }

    public async Task<IEnumerable<Voucher>> GetUserSavedVouchersAsync(Guid userId, bool isUsed = false, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<VoucherEntity>();
        var qf = new QueryFactory();
        
        var filter = new PredicateExpression(UserVoucherFields.UserId == userId);
        filter.AddWithAnd(UserVoucherFields.IsUsed == isUsed);
        
        var relations = new RelationCollection(VoucherEntity.Relations.UserVoucherEntityUsingVoucherId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            RelationsToUse = relations,
            FilterToUse = filter,
            SorterToUse = new SortExpression(UserVoucherFields.SavedAt.Descending())
        }, cancellationToken);

        return _mapper.Map<IEnumerable<Voucher>>(col);
    }

    public async Task<bool> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserVoucherEntity>();
        var filter = new PredicateExpression(UserVoucherFields.UserId == userId);
        filter.AddWithAnd(UserVoucherFields.VoucherId == voucherId);

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = filter,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        entity.IsUsed = true;
        entity.IsNew = false;
        
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }
}
