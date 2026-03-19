using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Reviews.Queries;

/// <summary>Lấy danh sách reviews của 1 product (public)</summary>
public record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<IEnumerable<ReviewDto>>>;

/// <summary>Lấy danh sách reviews của user hiện tại</summary>
public record GetMyReviewsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<IEnumerable<ReviewDto>>>;

/// <summary>Lấy rating tổng hợp của 1 product</summary>
public record GetProductRatingQuery(Guid ProductId) : IRequest<ApiResponse<ProductRatingDto>>;
