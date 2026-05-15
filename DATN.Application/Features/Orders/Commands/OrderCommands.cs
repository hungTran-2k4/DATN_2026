using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using MediatR;

namespace DATN.Application.Features.Orders.Commands;

/// <summary>
/// Checkout: Tạo đơn hàng từ các cart items được chọn.
/// Mỗi ShopId sẽ tạo ra 1 Order riêng.
/// </summary>
public class CheckoutCommand : IRequest<ApiResponse<IEnumerable<OrderSummaryDto>>>
{
    public Guid BuyerId { get; set; }

    /// <summary>Danh sách CartItem Ids được chọn để checkout</summary>
    public List<Guid> CartItemIds { get; set; } = new();

    /// <summary>AddressId từ sổ địa chỉ của user</summary>
    public Guid ShippingAddressId { get; set; }

    /// <summary>COD hoặc BANK_TRANSFER</summary>
    public string PaymentMethod { get; set; } = "COD";

    public string? CustomerNote { get; set; }
}

/// <summary>Buyer hủy đơn (chỉ được hủy khi status = PENDING)</summary>
public record CancelOrderCommand(Guid OrderId, Guid BuyerId, string? Reason = null) : IRequest<ApiResponse<bool>>;

/// <summary>Seller/Admin cập nhật trạng thái đơn hàng</summary>
public class UpdateOrderStatusCommand : IRequest<ApiResponse<bool>>
{
    public Guid OrderId { get; set; }
    public Guid ActorId { get; set; } // Seller or Admin userId
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
}

/// <summary>Buyer xác nhận đã nhận hàng (status chuyển từ DELIVERED sang COMPLETED)</summary>
public record ConfirmOrderReceivedCommand(Guid OrderId, Guid BuyerId) : IRequest<ApiResponse<bool>>;
