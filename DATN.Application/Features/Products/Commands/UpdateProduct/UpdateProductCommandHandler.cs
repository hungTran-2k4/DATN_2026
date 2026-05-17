using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Enums;
using DATN.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cache;

    public UpdateProductCommandHandler(IProductRepository productRepository, ICacheService cache)
    {
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, request.ShopId, cancellationToken);

        if (product == null)
        {
            return ApiResponse<bool>.Fail("Product not found or does not belong to this shop.", 404);
        }

        // Tùy chọn: Kiểm tra xem SKU hoặc Slug vừa sửa có trùng với sản phẩm khác (trong cùng shop) không
        var existingProduct = await _productRepository.GetBySkuOrSlugAsync(request.Sku, request.Slug, request.ShopId, cancellationToken);

        if (existingProduct != null && existingProduct.Id != request.Id)
        {
            return ApiResponse<bool>.Fail("Another product with the same SKU or Slug already exists.", 400);
        }

        // Cập nhật thông tin
        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Slug = request.Slug;
        product.Description = request.Description ?? string.Empty;
        product.Summary = request.Summary ?? string.Empty;
        // Chỉ cập nhật trạng thái nếu được cung cấp rõ ràng (dành cho Admin hoặc luồng cụ thể)
        // Seller thông thường sẽ không gửi Status qua form này nữa
        if (!string.IsNullOrEmpty(request.Status))
        {
            var newStatus = request.Status.ToProductStatus();
            
            // Bảo mật: Nếu sản phẩm đang Active hoặc Inactive, không cho phép hạ cấp về Draft qua luồng update thường
            // Hoặc đơn giản là ngăn chặn việc nâng cấp lên Active nếu không phải qua endpoint Review
            if (newStatus == ProductStatus.Active && product.Status != ProductStatus.Active && !request.BypassStatusCheck)
            {
                // Giữ nguyên trạng thái cũ nếu cố tình set Active ở đây
            }
            else
            {
                product.Status = newStatus;
            }
        }
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.ShopId = request.ShopId;
        product.BaseAttributes = request.BaseAttributes ?? "{}";
        product.UpdatedAt = DateTime.UtcNow;

        var saveResult = await _productRepository.UpdateAsync(product, cancellationToken);

        if (!saveResult)
        {
            return ApiResponse<bool>.Fail("Failed to update product.", 500);
        }

        _cache.RemoveByPrefix("products:");
        return ApiResponse<bool>.Succeed(true, "Product updated successfully.");
    }
}
