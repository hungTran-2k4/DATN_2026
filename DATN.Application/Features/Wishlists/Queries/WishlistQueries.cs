using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Wishlists.Queries;

/// <summary>Lấy danh sách sản phẩm yêu thích của user hiện tại</summary>
public record GetMyWishlistQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<IEnumerable<WishlistItemDto>>>;

/// <summary>Kiểm tra xem 1 product đã có trong wishlist chưa</summary>
public record CheckWishlistStatusQuery(Guid UserId, Guid ProductId) : IRequest<ApiResponse<bool>>;
