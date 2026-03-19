using DATN.Application.Common.Models;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.Features.Shops.Commands.DeleteShop;

public class DeleteShopCommandHandler : IRequestHandler<DeleteShopCommand, ApiResponse<bool>>
{
    private readonly IShopRepository _shopRepository;
    private readonly ICacheService _cache;

    public DeleteShopCommandHandler(IShopRepository shopRepository, ICacheService cache)
    {
        _shopRepository = shopRepository;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteShopCommand request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shop == null)
        {
            return ApiResponse<bool>.Fail("Shop not found.", 404);
        }

        // Kiểm tra quyền sở hữu Shop trước khi xóa
        if (request.OwnerId.HasValue && shop.OwnerId != request.OwnerId.Value)
        {
            return ApiResponse<bool>.Fail("You do not have permission to delete this shop.", 403);
        }

        var deleteResult = await _shopRepository.DeleteAsync(request.Id, cancellationToken);

        if (!deleteResult)
        {
            return ApiResponse<bool>.Fail("Failed to delete shop.", 500);
        }

        _cache.RemoveByPrefix("shops:");
        return ApiResponse<bool>.Succeed(true, "Shop deleted successfully.");
    }
}
