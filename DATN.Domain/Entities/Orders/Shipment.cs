using System;

namespace DATN.Domain.Entities.Orders;

/// <summary>
/// Thông tin vận chuyển — mỗi Order có tối đa 1 Shipment.
/// </summary>
public class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    
    /// <summary>Đơn vị vận chuyển: GHN, GHTK</summary>
    public string Provider { get; set; } = "GHN";
    
    /// <summary>Mã vận đơn do ĐVVC cấp</summary>
    public string? TrackingCode { get; set; }
    
    public decimal ShippingFee { get; set; }
    
    /// <summary>PENDING, PICKED, DELIVERING, DELIVERED, RETURNED, CANCELLED</summary>
    public string Status { get; set; } = "PENDING";
    
    public DateTime? ExpectedDeliveryDate { get; set; }
    
    /// <summary>Mã đơn nội bộ GHN</summary>
    public string? GhnOrderCode { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
