using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using DATN.Application.Features.Orders.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Orders.Handlers;

public class GetMyOrdersHandler : IRequestHandler<GetMyOrdersQuery, PagedResponse<IEnumerable<OrderSummaryDto>>>
{
    private readonly IOrderRepository _orderRepo;

    public GetMyOrdersHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<PagedResponse<IEnumerable<OrderSummaryDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _orderRepo.GetByBuyerIdAsync(
            request.BuyerId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            OrderStatus = o.OrderStatus,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            TotalAmount = o.TotalAmount,
            TotalItems = o.Items?.Count ?? 0,
            FirstItemName = o.Items?.FirstOrDefault()?.ProductNameSnapshot,
            CreatedAt = o.CreatedAt
        });

        return PagedResponse<IEnumerable<OrderSummaryDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}

public class GetShopOrdersHandler : IRequestHandler<GetShopOrdersQuery, PagedResponse<IEnumerable<OrderSummaryDto>>>
{
    private readonly IOrderRepository _orderRepo;

    public GetShopOrdersHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<PagedResponse<IEnumerable<OrderSummaryDto>>> Handle(GetShopOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _orderRepo.GetByShopIdAsync(
            request.ShopId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            OrderStatus = o.OrderStatus,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            TotalAmount = o.TotalAmount,
            TotalItems = o.Items?.Count ?? 0,
            FirstItemName = o.Items?.FirstOrDefault()?.ProductNameSnapshot,
            CreatedAt = o.CreatedAt
        });

        return PagedResponse<IEnumerable<OrderSummaryDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}

public class GetOrderDetailHandler : IRequestHandler<GetOrderDetailQuery, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepo;

    public GetOrderDetailHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<ApiResponse<OrderDto>> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderDto>.Fail("Không tìm thấy đơn hàng.", 404, "ORDER_NOT_FOUND");

        // MVP: cho phép buyer xem đơn của mình. Seller view sẽ bổ sung khi có GetShopOrders + check ownership theo shop.
        if (order.BuyerId != request.ActorId)
            return ApiResponse<OrderDto>.Fail("Không có quyền truy cập đơn hàng này.", 403, "ORDER_FORBIDDEN");

        return ApiResponse<OrderDto>.Succeed(new OrderDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            OrderStatus = order.OrderStatus,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            CustomerNote = order.CustomerNote,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                VariantId = i.VariantId,
                ProductNameSnapshot = i.ProductNameSnapshot,
                VariantName = i.VariantName,
                VariantImageUrl = i.VariantImageUrl,
                VariantAttributes = i.VariantAttributes,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        });
    }
}

