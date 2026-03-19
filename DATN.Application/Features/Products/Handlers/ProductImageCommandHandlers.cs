using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Products.Commands;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Products.Handlers;

public class UploadProductImageHandler : IRequestHandler<UploadProductImageCommand, ApiResponse<ProductImageDto>>
{
    private readonly IProductRepository _repo;

    public UploadProductImageHandler(IProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<ProductImageDto>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _repo.GetByIdAsync(request.ProductId, null, cancellationToken);
        if (product == null)
            return ApiResponse<ProductImageDto>.Fail("Không tìm thấy sản phẩm.", 404, "PRODUCT_NOT_FOUND");

        var image = new ProductImage
        {
            ProductId = request.ProductId,
            ImageUrl = request.ImageUrl,
            IsMain = request.IsMain
        };

        var created = await _repo.AddImageAsync(image, cancellationToken);

        if (request.IsMain)
        {
            await _repo.SetMainImageAsync(request.ProductId, created.Id, cancellationToken);
        }

        var dto = new ProductImageDto
        {
            Id = created.Id,
            ProductId = created.ProductId,
            ImageUrl = created.ImageUrl,
            DisplayOrder = created.DisplayOrder ?? 0,
            IsMain = created.IsMain ?? false
        };

        return ApiResponse<ProductImageDto>.Succeed(dto, "Thêm ảnh thành công.", 201);
    }
}

public class DeleteProductImageHandler : IRequestHandler<DeleteProductImageCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _repo;

    public DeleteProductImageHandler(IProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.DeleteImageAsync(request.ImageId, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Xóa ảnh thành công.")
            : ApiResponse<bool>.Fail("Không thể xóa ảnh.", 400, "DELETE_IMAGE_FAILED");
    }
}

public class SetMainProductImageHandler : IRequestHandler<SetMainProductImageCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _repo;

    public SetMainProductImageHandler(IProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<bool>> Handle(SetMainProductImageCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.SetMainImageAsync(request.ProductId, request.ImageId, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Đặt ảnh đại diện thành công.")
            : ApiResponse<bool>.Fail("Không thể cập nhật ảnh.", 400, "UPDATE_IMAGE_FAILED");
    }
}
