using System;
using System.Collections.Generic;
using DATN.Domain.Enums;

namespace DATN.Domain.Entities.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }

    /// <summary>Trạng thái sản phẩm — lưu dưới dạng string trong DB.</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    public int? ViewCount { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ShopId { get; set; }
    public string? BaseAttributes { get; set; }

    /// <summary>Điểm đánh giá trung bình (0.0 - 5.0)</summary>
    public decimal AverageRating { get; set; } = 0;

    /// <summary>Tổng số lượt đánh giá</summary>
    public int ReviewCount { get; set; } = 0;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
