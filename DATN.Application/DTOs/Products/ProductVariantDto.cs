namespace DATN.Application.DTOs.Products;

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Parsed variant attributes, e.g. {"color":"Đỏ","size":"XL"}</summary>
    public Dictionary<string, string>? VariantAttributes { get; set; }

    /// <summary>Số lượng tồn kho hiện tại</summary>
    public int StockQty { get; set; }
}
