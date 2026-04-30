using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using AutoMapper;

namespace DATN.Application.Features.Products.Handlers;

public class StockCommandHandlers :
    IRequestHandler<UpdateStockCommand, ApiResponse<StockDto>>,
    IRequestHandler<RestockCommand, ApiResponse<bool>>,
    IRequestHandler<ReserveStockCommand, ApiResponse<bool>>,
    IRequestHandler<CommitReservedStockCommand, ApiResponse<bool>>
{
    private readonly IStockRepository _stockRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public StockCommandHandlers(IStockRepository stockRepository, IMapper mapper, ICacheService cache)
    {
        _stockRepository = stockRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ApiResponse<StockDto>> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetStockByVariantIdAsync(request.VariantId, cancellationToken);
        if (stock == null) return ApiResponse<StockDto>.Fail("Stock not found.");

        stock.PhysicalQuantity = request.PhysicalQuantity;
        stock.AvailableQuantity = request.PhysicalQuantity - stock.ReservedQuantity;

        var success = await _stockRepository.UpdateStockAsync(stock, cancellationToken);
        if (!success) return ApiResponse<StockDto>.Fail("Failed to update stock.");

        // Record a transaction for administrative manual update
        await _stockRepository.AddTransactionAsync(new StockTransaction
        {
            VariantId = request.VariantId,
            Quantity = request.PhysicalQuantity, // For manual absolute updates, saving absolute value as quantity since we don't know the diff without more logic
            TransactionType = "ManualUpdate",
            Note = "Manual Stock Correction",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var dto = _mapper.Map<StockDto>(stock);
        _cache.RemoveByPrefix("products:");
        return ApiResponse<StockDto>.Succeed(dto, "Stock updated successfully");
    }

    public async Task<ApiResponse<bool>> Handle(RestockCommand request, CancellationToken cancellationToken)
    {
        var success = await _stockRepository.RestockAsync(request.VariantId, request.Quantity, cancellationToken);
        if (!success) return ApiResponse<bool>.Fail("Failed to restock");

        // Record Restock Transaction
        await _stockRepository.AddTransactionAsync(new StockTransaction
        {
            VariantId = request.VariantId,
            ShopId = request.ShopId,
            Quantity = request.Quantity,
            TransactionType = "Import",
            Note = request.Note ?? "Standard Restock",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        _cache.RemoveByPrefix("products:");
        return ApiResponse<bool>.Succeed(true, "Restocked successfully");
    }

    public async Task<ApiResponse<bool>> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var success = await _stockRepository.ReserveStockAsync(request.VariantId, request.Quantity, cancellationToken);
        if (!success) return ApiResponse<bool>.Fail("Failed to reserve stock. Insufficient quantity.");

        await _stockRepository.AddTransactionAsync(new StockTransaction
        {
            VariantId = request.VariantId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            TransactionType = "Reserve",
            Note = $"Reserved {request.Quantity} items for Order {request.ReferenceId}",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        _cache.RemoveByPrefix("products:");
        return ApiResponse<bool>.Succeed(true, "Stock reserved");
    }

    public async Task<ApiResponse<bool>> Handle(CommitReservedStockCommand request, CancellationToken cancellationToken)
    {
        var success = await _stockRepository.CommitReservedStockAsync(request.VariantId, request.Quantity, cancellationToken);
        if (!success) return ApiResponse<bool>.Fail("Failed to commit stock.");

        await _stockRepository.AddTransactionAsync(new StockTransaction
        {
            VariantId = request.VariantId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            TransactionType = "Sale",
            Note = $"Committed Sale for {request.Quantity} items for Order {request.ReferenceId}",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        _cache.RemoveByPrefix("products:");
        return ApiResponse<bool>.Succeed(true, "Stock committed");
    }
}
