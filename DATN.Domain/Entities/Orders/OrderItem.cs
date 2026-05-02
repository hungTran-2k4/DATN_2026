namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Chi tiết sản phẩm trong đơn hàng — snapshot tại thời điểm đặt hàng
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? VariantId { get; set; }

    /// <summary>Tên sản phẩm tại thời điểm đặt hàng (snapshot, không thay đổi dù seller sửa)</summary>
    public string? ProductNameSnapshot { get; set; }

    /// <summary>Giá bán tại thời điểm đặt hàng</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>Thành tiền = UnitPrice * Quantity</summary>
    public decimal SubTotal => UnitPrice * Quantity;

    // Additional info from Variant
    public string? VariantName { get; set; }
    public string? VariantImageUrl { get; set; }
    public string? VariantAttributes { get; set; }
}
