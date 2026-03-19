using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Products.Handlers;

public class GetProductImagesHandler : IRequestHandler<GetProductImagesQuery, ApiResponse<IEnumerable<ProductImageDto>>>
{
    private readonly IProductRepository _repo;

    public GetProductImagesHandler(IProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<IEnumerable<ProductImageDto>>> Handle(GetProductImagesQuery request, CancellationToken cancellationToken)
    {
        var product = await _repo.GetByIdAsync(request.ProductId, null, cancellationToken);
        if (product == null)
            return ApiResponse<IEnumerable<ProductImageDto>>.Fail("Không tìm thấy sản phẩm.", 404, "PRODUCT_NOT_FOUND");

        var images = await _repo.GetImagesAsync(request.ProductId, cancellationToken);
        
        var dtos = images.Select(i => new ProductImageDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ImageUrl = i.ImageUrl,
            DisplayOrder = i.DisplayOrder ?? 0,
            IsMain = i.IsMain ?? false
        });

        return ApiResponse<IEnumerable<ProductImageDto>>.Succeed(dtos);
    }
}
