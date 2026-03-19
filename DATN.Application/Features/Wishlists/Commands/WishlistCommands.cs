using DATN.Application.Common.Models;
using MediatR;

namespace DATN.Application.Features.Wishlists.Commands;

/// <summary>Thêm sản phẩm vào danh sách yêu thích</summary>
public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<ApiResponse<bool>>;

/// <summary>Xóa sản phẩm khỏi danh sách yêu thích</summary>
public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<ApiResponse<bool>>;
