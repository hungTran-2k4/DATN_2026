using DATN.Domain.Entities.Shops;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Interfaces;

public interface IShopRepository
{
    Task<IEnumerable<Shop>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Shop>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Shop> AddAsync(Shop shop, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Shop shop, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
