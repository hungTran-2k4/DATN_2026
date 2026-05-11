using System;

namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Yêu cầu rút tiền từ ví của Shop.
/// </summary>
public class WithdrawRequest
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public decimal Amount { get; set; }
    
    /// <summary>PENDING, APPROVED, COMPLETED, REJECTED</summary>
    public string Status { get; set; } = "PENDING";
    
    /// <summary>Thông tin ngân hàng nhận tiền (JSON)</summary>
    public string? BankInfo { get; set; }
    
    public string? Note { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public static class WithdrawStatus
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
}
