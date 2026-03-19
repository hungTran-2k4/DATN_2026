using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Reviews.Commands;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace DATN.Application.Features.Reviews.Handlers;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, ApiResponse<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IOrderRepository _orderRepo;

    public CreateReviewHandler(IReviewRepository reviewRepo, IOrderRepository orderRepo)
    {
        _reviewRepo = reviewRepo;
        _orderRepo = orderRepo;
    }

    public async Task<ApiResponse<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return ApiResponse<ReviewDto>.Fail("Rating phải từ 1 đến 5 sao.", 400, "INVALID_RATING");

        // Validate order exists and belongs to user
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null || order.BuyerId != request.UserId)
            return ApiResponse<ReviewDto>.Fail("Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu.", 404, "ORDER_NOT_FOUND");

        // Validate order is DELIVERED
        // Assuming OrderStatus was mapped to entity, if not, we check raw value
        if (order.OrderStatus != "DELIVERED" && order.OrderStatus != "COMPLETED")
            return ApiResponse<ReviewDto>.Fail("Bạn chỉ có thể đánh giá sản phẩm khi đơn hàng đã giao thành công.", 400, "ORDER_NOT_DELIVERED");

        // Validate variant is in order
        var orderItem = order.Items.FirstOrDefault(i => i.VariantId == request.VariantId);
        if (orderItem == null)
            return ApiResponse<ReviewDto>.Fail("Sản phẩm không có trong đơn hàng này.", 400, "VARIANT_NOT_IN_ORDER");

        // Check if user already reviewed
        var hasReviewed = await _reviewRepo.HasUserReviewedAsync(request.UserId, request.VariantId, request.OrderId, cancellationToken);
        if (hasReviewed)
            return ApiResponse<ReviewDto>.Fail("Bạn đã đánh giá sản phẩm này trong đơn hàng rồi.", 400, "ALREADY_REVIEWED");

        var review = new Review
        {
            UserId = request.UserId,
            VariantId = request.VariantId,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment,
            Images = request.Images != null && request.Images.Any() ? JsonSerializer.Serialize(request.Images) : null
        };

        var created = await _reviewRepo.CreateAsync(review, cancellationToken);

        var dto = new ReviewDto
        {
            Id = created.Id,
            UserId = created.UserId,
            VariantId = created.VariantId,
            OrderId = created.OrderId,
            Rating = created.Rating,
            Comment = created.Comment,
            Images = request.Images,
            CreatedAt = created.CreatedAt
        };

        return ApiResponse<ReviewDto>.Succeed(dto, "Đánh giá sản phẩm thành công.", 201);
    }
}

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, ApiResponse<bool>>
{
    private readonly IReviewRepository _reviewRepo;

    public DeleteReviewHandler(IReviewRepository reviewRepo)
    {
        _reviewRepo = reviewRepo;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null)
            return ApiResponse<bool>.Fail("Không tìm thấy đánh giá.", 404, "REVIEW_NOT_FOUND");

        if (review.UserId != request.UserId)
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa đánh giá này.", 403, "FORBIDDEN");

        var result = await _reviewRepo.DeleteAsync(request.ReviewId, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Đã xóa đánh giá thành công.")
            : ApiResponse<bool>.Fail("Không thể xóa lúc này.", 500, "SERVER_ERROR");
    }
}
