using AutoMapper;
using DATN.Domain.Entities.Identity;
using DATN.Domain.Interfaces;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;

namespace DATN.Infrastructure.Persistence.Repositories.Users;

public class UserAddressRepository : IUserAddressRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;

    public UserAddressRepository(DataAccessAdapter adapter, IMapper mapper)
    {
        _adapter = adapter;
        _mapper = mapper;
    }

    public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserAddressEntity>();
        var sorter = new SortExpression(UserAddressFields.IsDefault.Descending());
        sorter.Add(UserAddressFields.CreatedAt.Descending());

        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = UserAddressFields.UserId == userId,
            SorterToUse = sorter
        }, cancellationToken);
        return _mapper.Map<IEnumerable<UserAddress>>(col);
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserAddressEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = UserAddressFields.Id == id & UserAddressFields.UserId == userId,
            RowsToTake = 1
        }, cancellationToken);
        var entity = col.FirstOrDefault();
        return entity == null ? null : _mapper.Map<UserAddress>(entity);
    }

    public async Task<UserAddress> AddAsync(UserAddress address, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<UserAddressEntity>(address);
        entity.IsNew = true;
        await _adapter.SaveEntityAsync(entity, refetchAfterSave: true, cancellationToken: cancellationToken);
        return _mapper.Map<UserAddress>(entity);
    }

    public async Task<bool> UpdateAsync(UserAddress address, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserAddressEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = UserAddressFields.Id == address.Id & UserAddressFields.UserId == address.UserId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;

        _mapper.Map(address, entity);
        entity.IsNew = false;
        return await _adapter.SaveEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var col = new EntityCollection<UserAddressEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = UserAddressFields.Id == id & UserAddressFields.UserId == userId,
            RowsToTake = 1
        }, cancellationToken);

        var entity = col.FirstOrDefault();
        if (entity == null) return false;
        return await _adapter.DeleteEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> SetDefaultAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        // Step 1: Fetch all addresses of user
        var col = new EntityCollection<UserAddressEntity>();
        await _adapter.FetchEntityCollectionAsync(new QueryParameters
        {
            CollectionToFetch = col,
            FilterToUse = UserAddressFields.UserId == userId
        }, cancellationToken);

        // Step 2: Save all with IsDefault = false/true
        foreach (var addr in col)
        {
            var shouldBeDefault = addr.Id == id;
            if (addr.IsDefault != shouldBeDefault)
            {
                addr.IsDefault = shouldBeDefault;
                addr.IsNew = false;
                await _adapter.SaveEntityAsync(addr, cancellationToken: cancellationToken);
            }
        }

        return true;
    }
}
