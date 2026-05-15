using DATN.Domain.Entities.Orders;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Repository cho bảng payments — lưu lịch sử giao dịch chi tiết.
/// Dùng Npgsql trực tiếp (không qua LLBLGen) vì bảng payments được tạo riêng.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>Tạo bản ghi payment mới</summary>
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);

    /// <summary>Tìm payment theo OrderId (lấy bản ghi mới nhất)</summary>
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Tìm payment theo TransactionId từ gateway (cho idempotency check)</summary>
    Task<Payment?> GetByTransactionIdAsync(string transactionId, string provider, CancellationToken ct = default);

    /// <summary>Cập nhật trạng thái + response data sau khi nhận IPN</summary>
    Task<bool> UpdateAsync(Payment payment, CancellationToken ct = default);

    /// <summary>Tìm tất cả Payment records cùng nhóm thanh toán gộp (dựa trên RawResponse chứa groupKey)</summary>
    Task<IEnumerable<Payment>> GetByGroupKeyAsync(string groupKey, CancellationToken ct = default);
}
