namespace DATN.Application.DTOs.Categories;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public List<CategoryDto> Children { get; set; } = new();
}
