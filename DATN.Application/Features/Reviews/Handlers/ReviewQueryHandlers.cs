using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Reviews.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Reviews.Handlers;

public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, PagedResponse<IEnumerable<ReviewDto>>>
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IUserRepository _userRepo;

    public GetProductReviewsHandler(IReviewRepository reviewRepo, IUserRepository userRepo)
    {
        _reviewRepo = reviewRepo;
        _userRepo = userRepo;
    }

    public async Task<PagedResponse<IEnumerable<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _reviewRepo.GetByProductIdAsync(request.ProductId, request.Page, request.PageSize, cancellationToken);
        
        var dtos = new List<ReviewDto>();
        foreach (var r in items)
        {
            string? userName = null;
            if (r.UserId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(r.UserId.Value, cancellationToken);
                userName = user?.FullName ?? user?.Email ?? "Người dùng";
            }

            dtos.Add(new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = userName,
                VariantId = r.VariantId,
                OrderId = r.OrderId,
                Rating = r.Rating,
                Comment = r.Comment,
                Images = string.IsNullOrEmpty(r.Images) ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(r.Images),
                CreatedAt = r.CreatedAt
            });
        }

        return new PagedResponse<IEnumerable<ReviewDto>>(dtos, request.Page, request.PageSize, total);
    }
}

public class GetMyReviewsHandler : IRequestHandler<GetMyReviewsQuery, PagedResponse<IEnumerable<ReviewDto>>>
{
    private readonly IReviewRepository _reviewRepo;

    public GetMyReviewsHandler(IReviewRepository reviewRepo)
    {
        _reviewRepo = reviewRepo;
    }

    public async Task<PagedResponse<IEnumerable<ReviewDto>>> Handle(GetMyReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _reviewRepo.GetByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
        
        var dtos = items.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            VariantId = r.VariantId,
            OrderId = r.OrderId,
            Rating = r.Rating,
            Comment = r.Comment,
            Images = string.IsNullOrEmpty(r.Images) ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(r.Images),
            CreatedAt = r.CreatedAt
        });

        return new PagedResponse<IEnumerable<ReviewDto>>(dtos, request.Page, request.PageSize, total);
    }
}

public class GetProductRatingHandler : IRequestHandler<GetProductRatingQuery, ApiResponse<ProductRatingDto>>
{
    private readonly IReviewRepository _reviewRepo;

    public GetProductRatingHandler(IReviewRepository reviewRepo)
    {
        _reviewRepo = reviewRepo;
    }

    public async Task<ApiResponse<ProductRatingDto>> Handle(GetProductRatingQuery request, CancellationToken cancellationToken)
    {
        var (avg, total) = await _reviewRepo.GetProductRatingAsync(request.ProductId, cancellationToken);
        
        return ApiResponse<ProductRatingDto>.Succeed(new ProductRatingDto
        {
            ProductId = request.ProductId,
            AverageRating = avg,
            TotalReviews = total
        });
    }
}
