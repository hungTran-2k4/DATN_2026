using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Products.Handlers;

public class StockQueryHandlers :
    IRequestHandler<GetStockByVariantIdQuery, ApiResponse<StockDto>>,
    IRequestHandler<GetStocksByProductQuery, ApiResponse<IEnumerable<StockDto>>>,
    IRequestHandler<GetStockTransactionsByVariantQuery, PagedResponse<IEnumerable<StockTransactionDto>>>,
    IRequestHandler<GetStockTransactionsByShopQuery, PagedResponse<IEnumerable<StockTransactionDto>>>
{
    private readonly IStockRepository _stockRepository;
    private readonly IMapper _mapper;

    public StockQueryHandlers(IStockRepository stockRepository, IMapper mapper)
    {
        _stockRepository = stockRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<StockDto>> Handle(GetStockByVariantIdQuery request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetStockByVariantIdAsync(request.VariantId, cancellationToken);
        if (stock == null) return ApiResponse<StockDto>.Fail("Stock not found for variant.");

        var dto = _mapper.Map<StockDto>(stock);
        return ApiResponse<StockDto>.Succeed(dto);
    }

    public async Task<ApiResponse<IEnumerable<StockDto>>> Handle(GetStocksByProductQuery request, CancellationToken cancellationToken)
    {
        var stocks = await _stockRepository.GetStocksByProductAsync(request.ProductId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<StockDto>>(stocks);
        return ApiResponse<IEnumerable<StockDto>>.Succeed(dtos);
    }

    public async Task<PagedResponse<IEnumerable<StockTransactionDto>>> Handle(GetStockTransactionsByVariantQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _stockRepository.GetTransactionsByVariantAsync(request.VariantId, request.Page, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<StockTransactionDto>>(items);
        return PagedResponse<IEnumerable<StockTransactionDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }

    public async Task<PagedResponse<IEnumerable<StockTransactionDto>>> Handle(GetStockTransactionsByShopQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _stockRepository.GetTransactionsByShopAsync(request.ShopId, request.Page, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<StockTransactionDto>>(items);
        return PagedResponse<IEnumerable<StockTransactionDto>>.SucceedDefault(dtos, request.Page, request.PageSize, total);
    }
}
