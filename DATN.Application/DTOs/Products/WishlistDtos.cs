namespace DATN.Application.DTOs.Products;

/// <summary>Thông tin item trong wishlist (thường là ProductDto hoặc bản rút gọn)</summary>
public class WishlistItemDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? MainImageUrl { get; set; }
    public DateTime AddedAt { get; set; }
}
