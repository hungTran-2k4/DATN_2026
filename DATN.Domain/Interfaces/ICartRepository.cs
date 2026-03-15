using DATN.Domain.Entities.Orders;

namespace DATN.Domain.Interfaces;

public interface ICartRepository
{
    /// <summary>Lấy giỏ hàng của user (với enrichment data từ join product/variant/shop)</summary>
    Task<IEnumerable<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CartItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra variant đã có trong giỏ chưa.
    /// Nếu đã có → cập nhật quantity. Nếu chưa → thêm mới.
    /// </summary>
    Task<CartItem?> GetByVariantIdAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default);

    Task<CartItem> AddAsync(CartItem item, CancellationToken cancellationToken = default);
    Task<bool> UpdateQuantityAsync(Guid id, Guid userId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Xóa toàn bộ giỏ hàng của user (sau khi checkout)</summary>
    Task<bool> ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Xóa các cart items theo danh sách variantIds (sau khi checkout thành công)</summary>
    Task<bool> RemoveByVariantIdsAsync(Guid userId, IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default);
}
