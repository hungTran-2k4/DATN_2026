using System;

namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Sổ cái ví — ghi nhận chi tiết tăng/giảm tiền của Shop.
/// </summary>
public class WalletLedger
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid? TransactionId { get; set; }
    
    /// <summary>Số tiền biến động (ví dụ: +100000 hoặc -50000)</summary>
    public decimal Amount { get; set; }
    
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
