using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.ProductVariants.Commands;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace DATN.Application.Features.ProductVariants.Handlers;

public class CreateVariantHandler : IRequestHandler<CreateVariantCommand, ApiResponse<ProductVariantDto>>
{
    private readonly IProductVariantRepository _variantRepo;
    private readonly IProductRepository _productRepo;

    public CreateVariantHandler(IProductVariantRepository variantRepo, IProductRepository productRepo)
    {
        _variantRepo = variantRepo;
        _productRepo = productRepo;
    }

    public async Task<ApiResponse<ProductVariantDto>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate product belongs to this shop
        var product = await _productRepo.GetByIdAsync(request.ProductId, request.ShopId, cancellationToken);
        if (product == null)
            return ApiResponse<ProductVariantDto>.Fail("Sản phẩm không tồn tại hoặc không thuộc shop của bạn.", 403, "PRODUCT_FORBIDDEN");

        // 2. Validate SKU unique nếu có
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            if (await _variantRepo.SkuExistsAsync(request.Sku, cancellationToken: cancellationToken))
                return ApiResponse<ProductVariantDto>.Fail("SKU đã tồn tại trong hệ thống.", 400, "SKU_EXISTS");
        }

        // 3. Serialize variant attributes to JSON
        var attrJson = request.VariantAttributes != null
            ? JsonSerializer.Serialize(request.VariantAttributes)
            : null;

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Name = request.Name,
            Sku = request.Sku,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            VariantAttributes = attrJson,
            StockQty = request.InitialStock
        };

        var created = await _variantRepo.AddAsync(variant, cancellationToken);
        return ApiResponse<ProductVariantDto>.Succeed(MapToDto(created), "Tạo biến thể thành công.", 201);
    }

    private static ProductVariantDto MapToDto(ProductVariant v) => new()
    {
        Id = v.Id,
        ProductId = v.ProductId,
        Name = v.Name,
        Sku = v.Sku,
        Price = v.Price,
        ImageUrl = v.ImageUrl,
        VariantAttributes = v.VariantAttributes != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(v.VariantAttributes)
            : null,
        StockQty = v.StockQty
    };
}

public class UpdateVariantHandler : IRequestHandler<UpdateVariantCommand, ApiResponse<bool>>
{
    private readonly IProductVariantRepository _variantRepo;
    private readonly IProductRepository _productRepo;

    public UpdateVariantHandler(IProductVariantRepository variantRepo, IProductRepository productRepo)
    {
        _variantRepo = variantRepo;
        _productRepo = productRepo;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepo.GetByIdAsync(request.Id, cancellationToken);
        if (variant == null)
            return ApiResponse<bool>.Fail("Biến thể không tồn tại.", 404, "VARIANT_NOT_FOUND");

        // Verify ownership via product → shop
        var product = await _productRepo.GetByIdAsync(variant.ProductId!.Value, request.ShopId, cancellationToken);
        if (product == null)
            return ApiResponse<bool>.Fail("Không có quyền cập nhật biến thể này.", 403, "VARIANT_FORBIDDEN");

        if (!string.IsNullOrWhiteSpace(request.Sku) && request.Sku != variant.Sku)
        {
            if (await _variantRepo.SkuExistsAsync(request.Sku, excludeId: request.Id, cancellationToken: cancellationToken))
                return ApiResponse<bool>.Fail("SKU đã tồn tại.", 400, "SKU_EXISTS");
        }

        variant.Name = request.Name;
        variant.Sku = request.Sku;
        variant.Price = request.Price;
        variant.ImageUrl = request.ImageUrl;
        variant.VariantAttributes = request.VariantAttributes != null
            ? JsonSerializer.Serialize(request.VariantAttributes)
            : variant.VariantAttributes;

        await _variantRepo.UpdateAsync(variant, cancellationToken);
        return ApiResponse<bool>.Succeed(true, "Cập nhật biến thể thành công.");
    }
}

public class DeleteVariantHandler : IRequestHandler<DeleteVariantCommand, ApiResponse<bool>>
{
    private readonly IProductVariantRepository _variantRepo;
    private readonly IProductRepository _productRepo;

    public DeleteVariantHandler(IProductVariantRepository variantRepo, IProductRepository productRepo)
    {
        _variantRepo = variantRepo;
        _productRepo = productRepo;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, request.ShopId, cancellationToken);
        if (product == null)
            return ApiResponse<bool>.Fail("Sản phẩm không thuộc shop của bạn.", 403, "PRODUCT_FORBIDDEN");

        var result = await _variantRepo.DeleteAsync(request.Id, cancellationToken);
        return result
            ? ApiResponse<bool>.Succeed(true, "Đã xóa biến thể.")
            : ApiResponse<bool>.Fail("Không tìm thấy biến thể.", 404, "VARIANT_NOT_FOUND");
    }
}
