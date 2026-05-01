namespace DATN.Application.DTOs.Orders;

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid? VariantId { get; set; }
    public string? ProductNameSnapshot { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal => UnitPrice * Quantity;
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? OrderStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal? ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CustomerNote { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? OrderStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public string? FirstItemName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Shipping address snapshot được JSON serialize khi tạo order</summary>
public class ShippingAddressSnapshot
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DetailedAddress { get; set; } = string.Empty;
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
}
