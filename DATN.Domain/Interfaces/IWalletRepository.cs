using DATN.Domain.Entities.Orders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Interfaces;

public interface IWalletRepository
{
    Task<decimal> GetAvailableBalanceAsync(Guid shopId, CancellationToken ct = default);
    Task<decimal> GetLockedBalanceAsync(Guid shopId, CancellationToken ct = default);
    
    /// <summary>Cập nhật số dư và ghi sổ cái</summary>
    Task<bool> UpdateBalanceAsync(Guid shopId, decimal amount, string type, string description, Guid? transactionId = null, CancellationToken ct = default);
    
    /// <summary>Chuyển tiền từ tạm giữ sang khả dụng</summary>
    Task<bool> ReleaseLockedFundsAsync(Guid shopId, decimal amount, string description, CancellationToken ct = default);
    
    Task<IEnumerable<WalletLedger>> GetLedgersAsync(Guid shopId, int limit = 50, CancellationToken ct = default);
    
    /// <summary>Xử lý giải phóng các khoản ký quỹ đã quá hạn (7 ngày)</summary>
    Task ProcessEscrowReleaseAsync(CancellationToken ct = default);

    /// <summary>Hoàn trả tiền đang tạm giữ (khi hủy đơn)</summary>
    Task<bool> RefundLockedFundsAsync(Guid shopId, decimal amount, string description, CancellationToken ct = default);
}
