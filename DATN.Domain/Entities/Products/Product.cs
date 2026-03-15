using System;

namespace DATN.Domain.Entities.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? Status { get; set; }
    public int? ViewCount { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ShopId { get; set; }
    public string? BaseAttributes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
