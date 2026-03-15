using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<Guid>>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra Sku hoặc Slug đã tồn tại hay chưa (trong cùng một Shop)
        var existingProduct = await _productRepository.GetBySkuOrSlugAsync(request.Sku, request.Slug, request.ShopId, cancellationToken);
        
        if (existingProduct != null)
        {
            return ApiResponse<Guid>.Fail("Product with the same SKU or Slug already exists.", 400);
        }

        // 2. Tạo Entity mới
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Sku = request.Sku,
            Slug = request.Slug,
            Description = request.Description ?? string.Empty,
            Summary = request.Summary ?? string.Empty,
            Status = request.Status ?? "Active",
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            ShopId = request.ShopId,
            BaseAttributes = request.BaseAttributes ?? "{}",
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Lưu vào DB
        await _productRepository.AddAsync(product, cancellationToken);

        return ApiResponse<Guid>.Succeed(product.Id, "Product created successfully.");
    }
}
