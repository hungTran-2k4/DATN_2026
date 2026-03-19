namespace DATN.Domain.Entities.Products;

/// <summary>
/// Đánh giá sản phẩm — chỉ cho phép tài khoản đã mua thành công sản phẩm
/// được đánh giá 1-5 sao.
/// </summary>
public class Review
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? OrderId { get; set; }

    /// <summary>Điểm đánh giá 1-5 sao</summary>
    public int? Rating { get; set; }

    /// <summary>Nội dung bình luận</summary>
    public string? Comment { get; set; }

    /// <summary>Danh sách URL hình ảnh đính kèm (JSON array)</summary>
    public string? Images { get; set; }

    public DateTime? CreatedAt { get; set; }
}
