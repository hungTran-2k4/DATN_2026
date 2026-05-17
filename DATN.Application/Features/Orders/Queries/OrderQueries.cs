using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using MediatR;

namespace DATN.Application.Features.Orders.Queries;

public record GetMyOrdersQuery(Guid BuyerId, string? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResponse<IEnumerable<OrderSummaryDto>>>;

public record GetShopOrdersQuery(Guid ShopId, string? Status = null, string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResponse<IEnumerable<OrderSummaryDto>>>;

public record GetOrderDetailQuery(Guid OrderId, Guid ActorId, bool IsAdmin = false)
    : IRequest<ApiResponse<OrderDto>>;

