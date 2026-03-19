using DATN.Domain.Entities.Products;

namespace DATN.Domain.Interfaces;

public interface IReviewRepository
{
    /// <summary>Lấy danh sách reviews theo ProductId (qua VariantId), có phân trang</summary>
    Task<(IEnumerable<Review> Items, int Total)> GetByProductIdAsync(
        Guid productId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách reviews của 1 user</summary>
    Task<(IEnumerable<Review> Items, int Total)> GetByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy review theo Id</summary>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra user đã review variant trong order này chưa</summary>
    Task<bool> HasUserReviewedAsync(Guid userId, Guid variantId, Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Tạo review mới</summary>
    Task<Review> CreateAsync(Review review, CancellationToken cancellationToken = default);

    /// <summary>Xóa review (chỉ owner được xóa)</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tính trung bình rating và tổng số review của 1 product</summary>
    Task<(double AverageRating, int TotalReviews)> GetProductRatingAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
