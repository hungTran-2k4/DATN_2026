using DATN.Application.Common.Models;
using DATN.Application.DTOs.Categories;
using MediatR;
using System.Text.RegularExpressions;

namespace DATN.Application.Features.Categories.Commands;

public class CreateCategoryCommand : IRequest<ApiResponse<CategoryDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateCategoryCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public record DeactivateCategoryCommand(Guid Id) : IRequest<ApiResponse<bool>>;
