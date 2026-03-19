using DATN.Domain.Common;

namespace DATN.Domain.Entities.Products;

public class StockTransaction : BaseEntity
{
    public Guid? VariantId { get; set; }
    public Guid? ShopId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // e.g., "Import", "Export", "Sale", "Return"
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; } // Link to OrderId or ReturnId
    public string? Note { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation Properties
    public ProductVariant? ProductVariant { get; set; }
}
