using DATN.Domain.Entities.Products;

namespace DATN.Domain.Interfaces;

public interface IProductVariantRepository
{
    /// <summary>Lấy tất cả variants của 1 product (join với stock)</summary>
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách variant có phân trang, lọc và search</summary>
    Task<(IEnumerable<ProductVariant> Items, int Total)> GetPagedAsync(Guid? productId, string? search, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra SKU đã tồn tại trong hệ thống chưa</summary>
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<ProductVariant> AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra tồn kho — dùng trước khi thêm vào giỏ / đặt hàng</summary>
    Task<int> GetStockQtyAsync(Guid variantId, CancellationToken cancellationToken = default);

    /// <summary>Giảm tồn kho sau khi đặt hàng thành công (trong transaction cùng với tạo Order)</summary>
    Task<bool> DeductStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
}
