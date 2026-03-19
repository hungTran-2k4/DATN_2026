namespace DATN.Application.DTOs.Products;

/// <summary>Review response DTO</summary>
public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public Guid? OrderId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public List<string>? Images { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Thông tin rating tổng hợp của 1 product</summary>
public class ProductRatingDto
{
    public Guid ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
