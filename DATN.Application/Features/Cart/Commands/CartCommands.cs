using DATN.Application.Common.Models;
using DATN.Application.DTOs.Cart;
using MediatR;

namespace DATN.Application.Features.Cart.Commands;

/// <summary>Thêm sản phẩm vào giỏ. Nếu variant đã có → cộng dồn quantity</summary>
public class AddToCartCommand : IRequest<ApiResponse<CartItemDto>>
{
    public Guid UserId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Cập nhật số lượng của 1 cart item</summary>
public record UpdateCartItemCommand(Guid CartItemId, Guid UserId, int Quantity) : IRequest<ApiResponse<bool>>;

/// <summary>Xóa 1 item khỏi giỏ hàng</summary>
public record RemoveCartItemCommand(Guid CartItemId, Guid UserId) : IRequest<ApiResponse<bool>>;

/// <summary>Xóa toàn bộ giỏ hàng</summary>
public record ClearCartCommand(Guid UserId) : IRequest<ApiResponse<bool>>;
