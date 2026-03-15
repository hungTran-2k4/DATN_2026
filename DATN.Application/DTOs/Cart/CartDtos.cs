namespace DATN.Application.DTOs.Cart;

/// <summary>
/// Một item trong giỏ hàng, đã được enriched với thông tin product/variant
/// </summary>
public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
    public string? VariantName { get; set; }
    public string? VariantImageUrl { get; set; }
    public Dictionary<string, string>? VariantAttributes { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public int StockAvailable { get; set; }
}

/// <summary>
/// Giỏ hàng đã tách theo từng Shop — response khi GET /api/cart
/// </summary>
public class CartGroupDto
{
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(i => i.LineTotal);
}

public class CartDto
{
    public List<CartGroupDto> Groups { get; set; } = new();
    public int TotalItems => Groups.Sum(g => g.Items.Count);
    public decimal GrandTotal => Groups.Sum(g => g.SubTotal);
}
