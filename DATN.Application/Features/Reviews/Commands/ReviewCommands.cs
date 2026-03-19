using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Reviews.Commands;

/// <summary>
/// Tạo review cho sản phẩm đã mua.
/// Chỉ cho phép review khi đơn hàng có status = DELIVERED.
/// </summary>
public class CreateReviewCommand : IRequest<ApiResponse<ReviewDto>>
{
    public Guid UserId { get; set; }
    public Guid VariantId { get; set; }
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public List<string>? Images { get; set; }
}

/// <summary>Xóa review của mình</summary>
public record DeleteReviewCommand(Guid ReviewId, Guid UserId) : IRequest<ApiResponse<bool>>;
