using DATN.Domain.Common.Models;
using DATN.Domain.Entities.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Interfaces;

public interface IProductRepository
{
    Task<(IEnumerable<Product> Items, int Total)> GetPagedAsync(Guid? shopId = null, string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(Guid? shopId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, Guid? shopId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuOrSlugAsync(string sku, string slug, Guid? shopId = null, CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Product Images
    Task<IEnumerable<ProductImage>> GetImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductImage> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task<bool> SetMainImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
}
