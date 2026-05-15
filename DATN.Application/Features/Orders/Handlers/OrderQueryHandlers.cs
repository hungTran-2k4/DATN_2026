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
            ShopId = o.ShopId,
            ShopName = o.ShopName,
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
            ShopId = o.ShopId,
            ShopName = o.ShopName,
            CreatedAt = o.CreatedAt
        });

        return PagedResponse<IEnumerable<OrderSummaryDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}

public class GetAllOrdersHandler : IRequestHandler<GetAllOrdersQuery, PagedResponse<IEnumerable<OrderSummaryDto>>>
{
    private readonly IOrderRepository _orderRepo;

    public GetAllOrdersHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<PagedResponse<IEnumerable<OrderSummaryDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _orderRepo.GetAllAsync(
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
            ShopId = o.ShopId,
            ShopName = o.ShopName,
            CreatedAt = o.CreatedAt
        });

        return PagedResponse<IEnumerable<OrderSummaryDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}

public class GetOrderDetailHandler : IRequestHandler<GetOrderDetailQuery, ApiResponse<OrderDto>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IShopRepository _shopRepo;

    public GetOrderDetailHandler(IOrderRepository orderRepo, IShopRepository shopRepo)
    {
        _orderRepo = orderRepo;
        _shopRepo = shopRepo;
    }

    public async Task<ApiResponse<OrderDto>> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderDto>.Fail("Không tìm thấy đơn hàng.", 404, "ORDER_NOT_FOUND");

        bool isBuyer = order.BuyerId == request.ActorId;
        bool isSeller = false;

        if (order.ShopId.HasValue)
        {
            var shop = await _shopRepo.GetByIdAsync(order.ShopId.Value, cancellationToken);
            isSeller = shop != null && shop.OwnerId == request.ActorId;
        }

        if (!request.IsAdmin && !isBuyer && !isSeller)
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

