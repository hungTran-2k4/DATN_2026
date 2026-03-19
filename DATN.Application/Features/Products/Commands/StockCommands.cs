using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Products.Commands;

// Administrative update of physical stock (bypassing transactions if needed, though transactions are preferred)
public class UpdateStockCommand : IRequest<ApiResponse<StockDto>>
{
    public Guid VariantId { get; set; }
    public int PhysicalQuantity { get; set; }
}

public class RestockCommand : IRequest<ApiResponse<bool>>
{
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public Guid? ShopId { get; set; }
    public string? Note { get; set; }
}

public class ReserveStockCommand : IRequest<ApiResponse<bool>>
{
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; } // OrderId
}

public class CommitReservedStockCommand : IRequest<ApiResponse<bool>>
{
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; }
}
