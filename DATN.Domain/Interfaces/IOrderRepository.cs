using DATN.Domain.Entities.Orders;

namespace DATN.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);

    /// <summary>Lịch sử mua hàng của Buyer — có phân trang</summary>
    Task<(IEnumerable<Order> Items, int Total)> GetByBuyerIdAsync(
        Guid buyerId,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Đơn hàng của Shop (Seller view) — có phân trang</summary>
    Task<(IEnumerable<Order> Items, int Total)> GetByShopIdAsync(
        Guid shopId,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Tạo nhiều đơn cùng lúc (1 checkout → nhiều order, 1 per shop)</summary>
    Task<IEnumerable<Order>> CreateBulkAsync(IEnumerable<Order> orders, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật trạng thái đơn hàng — dùng sau khi validate transition</summary>
    Task<bool> UpdateStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>Sinh OrderCode dạng ORD-YYYYMMDD-{random4} đảm bảo unique</summary>
    Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken = default);
}
