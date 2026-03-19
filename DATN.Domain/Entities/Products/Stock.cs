using DATN.Domain.Common;

namespace DATN.Domain.Entities.Products;

public class Stock : BaseEntity
{
    public int PhysicalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int? AvailableQuantity { get; set; }
    
    // In BaseEntity Id maps to VariantId
    public DateTime? UpdatedAt { get; set; }

    // Navigation Property
    public ProductVariant? ProductVariant { get; set; }
}
