using AutoMapper;
using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Products;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ICacheService _cache;

    public CreateProductCommandHandler(IProductRepository productRepository, IBrandRepository brandRepository, ICacheService cache)
    {
        _productRepository = productRepository;
        _brandRepository = brandRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra Sku hoặc Slug đã tồn tại hay chưa (trong cùng một Shop)
        var existingProduct = await _productRepository.GetBySkuOrSlugAsync(request.Sku, request.Slug, request.ShopId, cancellationToken);

        if (existingProduct != null)
        {
            return ApiResponse<Guid>.Fail("Product with the same SKU or Slug already exists.", 400);
        }

        // Kiểm tra BrandId nếu có
        if (request.BrandId.HasValue)
        {
            var brand = await _brandRepository.GetByIdAsync(request.BrandId.Value, cancellationToken);
            if (brand == null) return ApiResponse<Guid>.Fail("Brand does not exist.", 404);
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
            Status = ProductStatus.Draft,
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
        _cache.RemoveByPrefix("products:");

        return ApiResponse<Guid>.Succeed(product.Id, "Product created successfully.");
    }
}
