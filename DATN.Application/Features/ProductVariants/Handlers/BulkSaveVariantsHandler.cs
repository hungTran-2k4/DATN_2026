using DATN.Application.Common.Models;
using DATN.Application.Features.ProductVariants.Commands;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace DATN.Application.Features.ProductVariants.Handlers;

public class BulkSaveVariantsHandler : IRequestHandler<BulkSaveVariantsCommand, ApiResponse<bool>>
{
    private readonly IProductVariantRepository _variantRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICacheService _cache;

    public BulkSaveVariantsHandler(IProductVariantRepository variantRepo, IProductRepository productRepo, ICacheService cache)
    {
        _variantRepo = variantRepo;
        _productRepo = productRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(BulkSaveVariantsCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate product belongs to this shop
        var product = await _productRepo.GetByIdAsync(request.ProductId, request.ShopId, cancellationToken);
        if (product == null)
            return ApiResponse<bool>.Fail("Sản phẩm không tồn tại hoặc không thuộc shop của bạn.", 403, "PRODUCT_FORBIDDEN");

        if (request.Variants == null || !request.Variants.Any())
            return ApiResponse<bool>.Succeed(true, "Không có thay đổi nào.", 200);

        var creates = new List<ProductVariant>();
        var updates = new List<ProductVariant>();

        foreach (var item in request.Variants)
        {
            var attrJson = item.VariantAttributes != null
                ? JsonSerializer.Serialize(item.VariantAttributes)
                : null;

            if (!item.Id.HasValue || item.Id.Value == Guid.Empty)
            {
                // Create
                creates.Add(new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    Name = item.Name,
                    Sku = item.Sku,
                    Price = item.Price,
                    OriginalPrice = item.OriginalPrice,
                    ImageUrl = item.ImageUrl,
                    VariantAttributes = attrJson,
                    StockQty = item.InitialStock
                });
            }
            else
            {
                // Update
                updates.Add(new ProductVariant
                {
                    Id = item.Id.Value,
                    ProductId = request.ProductId,
                    Name = item.Name,
                    Sku = item.Sku,
                    Price = item.Price,
                    OriginalPrice = item.OriginalPrice,
                    ImageUrl = item.ImageUrl,
                    VariantAttributes = attrJson
                });
            }
        }

        // 3. Execute bulk save
        await _variantRepo.BulkSaveAsync(creates, updates, cancellationToken);

        // 4. Clear cache
        _cache.RemoveByPrefix("products:");
        _cache.RemoveByPrefix($"product:{request.ProductId}");

        return ApiResponse<bool>.Succeed(true, "Lưu cấu hình biến thể thành công.", 200);
    }
}
