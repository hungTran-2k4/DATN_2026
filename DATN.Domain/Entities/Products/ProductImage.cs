namespace DATN.Domain.Entities.Products;

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public bool? IsMain { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
}
