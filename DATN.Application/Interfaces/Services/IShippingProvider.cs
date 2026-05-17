using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces.Services;

/// <summary>
/// Abstraction cho đơn vị vận chuyển — Strategy Pattern.
/// Mỗi carrier (GHN, GHTK, ...) implement interface này.
/// </summary>
public interface IShippingProvider
{
    string ProviderName { get; }

    /// <summary>Tính phí vận chuyển</summary>
    Task<ShippingFeeResult> CalculateFeeAsync(ShippingFeeRequest request);

    /// <summary>Tạo vận đơn (khi Seller gửi hàng)</summary>
    Task<CreateShipmentResult> CreateShipmentAsync(CreateShipmentRequest request);

    /// <summary>Hủy vận đơn đã tạo</summary>
    Task<bool> CancelShipmentAsync(string ghnOrderCode);
}

// ─── DTOs ──────────────────────────────────────────

public class ShippingFeeRequest
{
    public Guid? ShopId { get; set; }
    public int FromDistrictId { get; set; }
    public string FromWardCode { get; set; } = "";
    public int ToDistrictId { get; set; }
    public string ToWardCode { get; set; } = "";
    public int Weight { get; set; } = 500;         // gram
    public int InsuranceValue { get; set; }          // VND
    public int ServiceTypeId { get; set; } = 2;      // 2 = E-Commerce Delivery
}

public class ShippingFeeResult
{
    public bool Success { get; set; }
    public decimal TotalFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public string? Message { get; set; }
}

public class CreateShipmentRequest
{
    // Thông tin người gửi (Shop)
    public string FromName { get; set; } = "";
    public string FromPhone { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public int FromDistrictId { get; set; }
    public string FromWardCode { get; set; } = "";

    // Thông tin người nhận (Buyer)
    public string ToName { get; set; } = "";
    public string ToPhone { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public int ToDistrictId { get; set; }
    public string ToWardCode { get; set; } = "";

    // Thông tin đơn hàng
    public string ClientOrderCode { get; set; } = "";  // = OrderCode
    public decimal CodAmount { get; set; }              // Tiền thu hộ (COD)
    public int Weight { get; set; } = 500;
    public int InsuranceValue { get; set; }
    public string? Note { get; set; }

    // Danh sách sản phẩm
    public List<ShipmentItem> Items { get; set; } = new();

    /// <summary>Ca lấy hàng GHN (optional). Ví dụ: [2] = Ca chiều.</summary>
    public List<int>? PickShift { get; set; }
}

public class ShipmentItem
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int Weight { get; set; } = 500;
}

public class CreateShipmentResult
{
    public bool Success { get; set; }
    public string? TrackingCode { get; set; }
    public string? GhnOrderCode { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalFee { get; set; }
    public string? Message { get; set; }
}
