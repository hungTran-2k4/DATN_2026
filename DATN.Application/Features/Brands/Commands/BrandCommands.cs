using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Brands.Commands;

public class CreateBrandCommand : IRequest<ApiResponse<BrandDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateBrandCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
}

public record DeleteBrandCommand(Guid Id) : IRequest<ApiResponse<bool>>;
