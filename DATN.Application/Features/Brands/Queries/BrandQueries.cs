using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using MediatR;

namespace DATN.Application.Features.Brands.Queries;

public record GetBrandsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<IEnumerable<BrandDto>>>;

public record GetAllActiveBrandsQuery() : IRequest<ApiResponse<IEnumerable<BrandDto>>>;

public record GetBrandByIdQuery(Guid Id) : IRequest<ApiResponse<BrandDto>>;
