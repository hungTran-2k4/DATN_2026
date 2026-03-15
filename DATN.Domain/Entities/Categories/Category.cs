namespace DATN.Domain.Entities.Categories;

/// <summary>
/// Danh mục sản phẩm hỗ trợ phân cấp (cha → con)
/// </summary>
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }

    /// <summary>Danh mục cha (null = danh mục gốc Level-1)</summary>
    public Guid? ParentId { get; set; }

    public bool? IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
