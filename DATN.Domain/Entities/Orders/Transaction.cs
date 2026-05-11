using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Domain.Entities.Orders;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? ExternalTransactionNo { get; set; }
    public decimal Amount { get; set; }
    public string? Provider { get; set; }
    public string? Status { get; set; } // Success, Failed, Pending
    public string? RawResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TransactionType { get; set; } // PAYMENT_IN, PAYOUT, REFUND
    public string? ReferenceId { get; set; }
}
