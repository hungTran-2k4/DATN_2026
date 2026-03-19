using DATN.Domain.Entities.Products;

namespace DATN.Domain.Interfaces;

public interface IWishlistRepository
{
    /// <summary>Lấy danh sách yêu thích của user, bao gồm thông tin chi tiết của Product</summary>
    Task<(IEnumerable<Product> Items, int Total)> GetProductsByUserIdAsync(
        Guid userId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra product đã nằm trong wishlist của user chưa</summary>
    Task<bool> ExistsAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Thêm vào wishlist</summary>
    Task<bool> AddAsync(WishlistItem item, CancellationToken cancellationToken = default);

    /// <summary>Xóa khỏi wishlist</summary>
    Task<bool> RemoveAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Đếm tổng số lượt yêu thích của 1 product (tùy chọn)</summary>
    Task<int> GetProductWishlistCountAsync(Guid productId, CancellationToken cancellationToken = default);
}
