using DATN.Application.Common.Models;
using DATN.Application.Features.Wishlists.Commands;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Wishlists.Handlers;

public class AddToWishlistHandler : IRequestHandler<AddToWishlistCommand, ApiResponse<bool>>
{
    private readonly IWishlistRepository _wishlistRepo;

    public AddToWishlistHandler(IWishlistRepository wishlistRepo)
    {
        _wishlistRepo = wishlistRepo;
    }

    public async Task<ApiResponse<bool>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var exists = await _wishlistRepo.ExistsAsync(request.UserId, request.ProductId, cancellationToken);
        if (exists)
            return ApiResponse<bool>.Fail("Sản phẩm đã có trong danh sách yêu thích.", 400, "ALREADY_IN_WISHLIST");

        var item = new WishlistItem
        {
            UserId = request.UserId,
            ProductId = request.ProductId
        };

        var result = await _wishlistRepo.AddAsync(item, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Đã thêm vào danh sách yêu thích.")
            : ApiResponse<bool>.Fail("Không thể thêm lúc này.", 500, "SERVER_ERROR");
    }
}

public class RemoveFromWishlistHandler : IRequestHandler<RemoveFromWishlistCommand, ApiResponse<bool>>
{
    private readonly IWishlistRepository _wishlistRepo;

    public RemoveFromWishlistHandler(IWishlistRepository wishlistRepo)
    {
        _wishlistRepo = wishlistRepo;
    }

    public async Task<ApiResponse<bool>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var result = await _wishlistRepo.RemoveAsync(request.UserId, request.ProductId, cancellationToken);
        // Note: LLBLGen delete won't fail if item isn't there, it just returns false or true. 
        // We will return success anyway since goal is item removed.
        return ApiResponse<bool>.Succeed(true, "Đã xóa khỏi danh sách yêu thích.");
    }
}
