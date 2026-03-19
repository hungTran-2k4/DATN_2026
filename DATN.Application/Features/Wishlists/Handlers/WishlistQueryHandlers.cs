using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Wishlists.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Wishlists.Handlers;

public class GetMyWishlistHandler : IRequestHandler<GetMyWishlistQuery, PagedResponse<IEnumerable<WishlistItemDto>>>
{
    private readonly IWishlistRepository _wishlistRepo;

    public GetMyWishlistHandler(IWishlistRepository wishlistRepo)
    {
        _wishlistRepo = wishlistRepo;
    }

    public async Task<PagedResponse<IEnumerable<WishlistItemDto>>> Handle(GetMyWishlistQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _wishlistRepo.GetProductsByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
        
        var dtos = items.Select(p => new WishlistItemDto
        {
            ProductId = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            Slug = p.Slug,
            Price = null, // Can join to variants physically if needed, or fetched alongside
            // If the LLBLGen query joins variant for min price, it can be mapped here. 
            // Currently left null, assuming client fetches details on click.
            AddedAt = DateTime.UtcNow // Exact time requires WishlistItem return model. Simplifying for now.
        });

        return new PagedResponse<IEnumerable<WishlistItemDto>>(dtos, request.Page, request.PageSize, total);
    }
}

public class CheckWishlistStatusHandler : IRequestHandler<CheckWishlistStatusQuery, ApiResponse<bool>>
{
    private readonly IWishlistRepository _wishlistRepo;

    public CheckWishlistStatusHandler(IWishlistRepository wishlistRepo)
    {
        _wishlistRepo = wishlistRepo;
    }

    public async Task<ApiResponse<bool>> Handle(CheckWishlistStatusQuery request, CancellationToken cancellationToken)
    {
        var exists = await _wishlistRepo.ExistsAsync(request.UserId, request.ProductId, cancellationToken);
        return ApiResponse<bool>.Succeed(exists);
    }
}
