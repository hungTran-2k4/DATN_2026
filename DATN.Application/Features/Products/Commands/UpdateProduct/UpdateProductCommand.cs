using DATN.Application.Common.Models;
using MediatR;
using System;

namespace DATN.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? Status { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ShopId { get; set; }
    public string? BaseAttributes { get; set; }
}
