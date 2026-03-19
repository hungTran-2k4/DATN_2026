using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Brands.Queries;
using DATN.Domain.Interfaces;
using MediatR;

namespace DATN.Application.Features.Brands.Handlers;

public class GetBrandsHandler : IRequestHandler<GetBrandsQuery, PagedResponse<IEnumerable<BrandDto>>>
{
    private readonly IBrandRepository _repo;
    public GetBrandsHandler(IBrandRepository repo) => _repo = repo;

    public async Task<PagedResponse<IEnumerable<BrandDto>>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repo.GetPagedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        
        var dtos = items.Select(x => new BrandDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            LogoUrl = x.LogoUrl,
            IsActive = x.IsActive ?? true
        });

        return new PagedResponse<IEnumerable<BrandDto>>(dtos, request.Page, request.PageSize, total);
    }
}

public class GetAllActiveBrandsHandler : IRequestHandler<GetAllActiveBrandsQuery, ApiResponse<IEnumerable<BrandDto>>>
{
    private readonly IBrandRepository _repo;
    public GetAllActiveBrandsHandler(IBrandRepository repo) => _repo = repo;

    public async Task<ApiResponse<IEnumerable<BrandDto>>> Handle(GetAllActiveBrandsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repo.GetAllActiveAsync(cancellationToken);
        
        var dtos = items.Select(x => new BrandDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            LogoUrl = x.LogoUrl,
            IsActive = x.IsActive ?? true
        });

        return ApiResponse<IEnumerable<BrandDto>>.Succeed(dtos);
    }
}

public class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, ApiResponse<BrandDto>>
{
    private readonly IBrandRepository _repo;
    public GetBrandByIdHandler(IBrandRepository repo) => _repo = repo;

    public async Task<ApiResponse<BrandDto>> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repo.GetByIdAsync(request.Id, cancellationToken);
        
        if (item == null)
            return ApiResponse<BrandDto>.Fail("Không tìm thấy thương hiệu.", 404, "BRAND_NOT_FOUND");

        var dto = new BrandDto
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            LogoUrl = item.LogoUrl,
            IsActive = item.IsActive ?? true
        };

        return ApiResponse<BrandDto>.Succeed(dto);
    }
}
