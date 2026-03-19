namespace DATN.Application.DTOs.Products;

public class StockDto
{
    public Guid VariantId { get; set; }
    public int PhysicalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int? AvailableQuantity { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? ShopId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }
    public DateTime? CreatedAt { get; set; }
}
