namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Đơn hàng — mỗi Order thuộc về 1 Buyer và 1 Shop.
/// Khi Checkout với goods từ nhiều Shop → tạo nhiều Order riêng.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    /// <summary>Mã đơn hàng hiển thị: format ORD-YYYYMMDD-XXXX, unique</summary>
    public string OrderCode { get; set; } = string.Empty;

    public Guid? BuyerId { get; set; }

    /// <summary>
    /// Địa chỉ giao hàng snapshot dạng JSON (tránh bị ảnh hưởng khi user xóa address)
    /// Ví dụ: {"fullName":"Nguyễn A", "phone":"0901234567", "address":"123 Lê Lợi, P.1, Q.1, TP.HCM"}
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Phương thức thanh toán: COD | BANK_TRANSFER</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Trạng thái thanh toán: UNPAID | PAID | REFUNDED</summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Trạng thái đơn hàng:
    /// PENDING → CONFIRMED → PREPARING → SHIPPING → DELIVERED → CANCELLED
    /// </summary>
    public string? OrderStatus { get; set; }

    public decimal? ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }

    public string? CustomerNote { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

/// <summary>Các trạng thái hợp lệ của đơn hàng</summary>
public static class OrderStatus
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Preparing = "PREPARING";
    public const string Shipping = "SHIPPING";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";

    private static readonly string[] SellerCanTransition = { Confirmed, Preparing, Shipping, Delivered, Cancelled };

    /// <summary>Kiểm tra luồng chuyển trạng thái hợp lệ</summary>
    public static bool IsValidTransition(string current, string next)
    {
        return (current, next) switch
        {
            (Pending, Confirmed) => true,
            (Pending, Cancelled) => true,
            (Confirmed, Preparing) => true,
            (Confirmed, Cancelled) => true,
            (Preparing, Shipping) => true,
            (Shipping, Delivered) => true,
            _ => false
        };
    }
}

/// <summary>Phương thức thanh toán</summary>
public static class PaymentMethod
{
    public const string Cod = "COD";
    public const string BankTransfer = "BANK_TRANSFER";
}
