using DATN.Application.Common.Models;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, request.ShopId, cancellationToken);
        if (product == null)
        {
            return ApiResponse<bool>.Fail("Product not found or does not belong to this shop.", 404);
        }

        var deleteResult = await _productRepository.DeleteAsync(request.Id, cancellationToken);

        if (!deleteResult)
        {
            return ApiResponse<bool>.Fail("Failed to delete product.", 500);
        }

        return ApiResponse<bool>.Succeed(true, "Product deleted successfully.");
    }
}
