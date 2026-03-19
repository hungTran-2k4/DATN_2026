namespace DATN.Domain.Entities.Marketing;

public class Voucher
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty; // e.g. "Percentage", "FixedAmount"
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int? UsedCount { get; set; } = 0;
    public bool? IsActive { get; set; } = true;
    public Guid? ShopId { get; set; } // Null if it's a global platform voucher
}
