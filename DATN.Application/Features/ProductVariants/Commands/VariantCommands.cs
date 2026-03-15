using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.ProductVariants.Commands;

/// <summary>Tạo biến thể mới cho sản phẩm (Seller only)</summary>
public class CreateVariantCommand : IRequest<ApiResponse<ProductVariantDto>>
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; } // dùng để verify ownership
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, string>? VariantAttributes { get; set; }
    public int InitialStock { get; set; }
}

/// <summary>Cập nhật biến thể</summary>
public class UpdateVariantCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, string>? VariantAttributes { get; set; }
}

public record DeleteVariantCommand(Guid Id, Guid ShopId, Guid ProductId) : IRequest<ApiResponse<bool>>;
