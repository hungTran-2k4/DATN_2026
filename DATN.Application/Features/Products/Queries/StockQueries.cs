using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Products.Queries;

public record GetStockByVariantIdQuery(Guid VariantId) : IRequest<ApiResponse<StockDto>>;
public record GetStocksByProductQuery(Guid ProductId) : IRequest<ApiResponse<IEnumerable<StockDto>>>;

public record GetStockTransactionsByVariantQuery(Guid VariantId, int Page = 1, int PageSize = 20) 
    : IRequest<PagedResponse<IEnumerable<StockTransactionDto>>>;

public record GetStockTransactionsByShopQuery(Guid ShopId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResponse<IEnumerable<StockTransactionDto>>>;
