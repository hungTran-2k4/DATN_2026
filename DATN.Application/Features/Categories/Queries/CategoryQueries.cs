using DATN.Application.Common.Models;
using DATN.Application.DTOs.Categories;
using MediatR;

namespace DATN.Application.Features.Categories.Queries;

public record GetCategoriesQuery(bool ActiveOnly = true) : IRequest<ApiResponse<IEnumerable<CategoryDto>>>;
public record GetCategoryByIdQuery(Guid Id) : IRequest<ApiResponse<CategoryDto>>;
