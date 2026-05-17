using DATN.Application.Common.Models;
using DATN.Application.DTOs.Orders;
using MediatR;

namespace DATN.Application.Features.Orders.Queries;

public record GetAllOrdersQuery(string? Status, string? Search, int Page, int PageSize) : IRequest<PagedResponse<IEnumerable<OrderSummaryDto>>>;
