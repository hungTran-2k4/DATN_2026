namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Cart item - mỗi record là 1 sản phẩm (variant) trong giỏ hàng của người dùng.
/// Bảng DB: carts (UserId, VariantId, Quantity)
/// </summary>
public class CartItem
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime? CreatedAt { get; set; }

    // -- Enrichment (populated when reading, not stored) --
    /// <summary>ShopId của variant → dùng để group giỏ hàng theo shop</summary>
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
    public string? VariantName { get; set; }
    public string? VariantImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public string? VariantAttributes { get; set; }
    public int StockQty { get; set; }
}
