using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Products.Commands;

public class UploadProductImageCommand : IRequest<ApiResponse<ProductImageDto>>
{
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ApiResponse<bool>>;

public record SetMainProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ApiResponse<bool>>;
