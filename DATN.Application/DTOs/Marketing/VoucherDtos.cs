namespace DATN.Application.DTOs.Marketing;

public class VoucherDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public Guid? ShopId { get; set; }
}

public class UserVoucherDto
{
    public Guid UserId { get; set; }
    public Guid VoucherId { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? SavedAt { get; set; }
    public VoucherDto Voucher { get; set; } = default!;
}
