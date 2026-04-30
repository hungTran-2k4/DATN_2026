namespace DATN.Domain.Entities.Products;

/// <summary>
/// Biến thể sản phẩm: quản lý màu sắc, size, giá, SKU, tồn kho riêng
/// </summary>
public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }

    /// <summary>Tên biến thể, ví dụ: "Đỏ - XL"</summary>
    public string? Name { get; set; }

    /// <summary>SKU riêng của biến thể, phải unique trên toàn hệ thống</summary>
    public string? Sku { get; set; }

    /// <summary>Giá bán của biến thể (bắt buộc)</summary>
    public decimal Price { get; set; }

    /// <summary>Giá gốc của biến thể (để hiển thị giảm giá)</summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>Hình ảnh riêng của biến thể</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Thuộc tính dạng JSON, ví dụ: {"color":"Đỏ","size":"XL"}
    /// Lưu dạng JSONB trong PostgreSQL
    /// </summary>
    public string? VariantAttributes { get; set; }

    /// <summary>Số lượng tồn kho (được join từ bảng stocks)</summary>
    public int StockQty { get; set; }
}
