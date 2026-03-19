using DATN.Application.Common.Models;
using DATN.Application.DTOs.Products;
using DATN.Application.Features.Brands.Commands;
using DATN.Domain.Entities.Products;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.RegularExpressions;

namespace DATN.Application.Features.Brands.Handlers;

public class CreateBrandHandler : IRequestHandler<CreateBrandCommand, ApiResponse<BrandDto>>
{
    private readonly IBrandRepository _repo;
    public CreateBrandHandler(IBrandRepository repo) => _repo = repo;

    public async Task<ApiResponse<BrandDto>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug.Trim().ToLower();

        if (await _repo.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            return ApiResponse<BrandDto>.Fail("Slug đã tồn tại.", 400, "SLUG_EXISTS");

        var brand = new Brand
        {
            Name = request.Name,
            Slug = slug,
            LogoUrl = request.LogoUrl,
            IsActive = true
        };

        var created = await _repo.AddAsync(brand, cancellationToken);

        var dto = new BrandDto
        {
            Id = created.Id,
            Name = created.Name,
            Slug = created.Slug,
            LogoUrl = created.LogoUrl,
            IsActive = created.IsActive ?? true
        };

        return ApiResponse<BrandDto>.Succeed(dto, "Tạo thương hiệu thành công.", 201);
    }

    private static string GenerateSlug(string name) =>
        Regex.Replace(name.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');
}

public class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, ApiResponse<bool>>
{
    private readonly IBrandRepository _repo;
    public UpdateBrandHandler(IBrandRepository repo) => _repo = repo;

    public async Task<ApiResponse<bool>> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return ApiResponse<bool>.Fail("Không tìm thấy thương hiệu.", 404, "BRAND_NOT_FOUND");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? existing.Slug
            : request.Slug.Trim().ToLower();

        if (await _repo.SlugExistsAsync(slug, request.Id, cancellationToken))
            return ApiResponse<bool>.Fail("Slug đã tồn tại cho một thương hiệu khác.", 400, "SLUG_EXISTS");

        existing.Name = request.Name;
        existing.Slug = slug;
        existing.LogoUrl = request.LogoUrl;
        existing.IsActive = request.IsActive;

        var result = await _repo.UpdateAsync(existing, cancellationToken);
        return result 
            ? ApiResponse<bool>.Succeed(true, "Cập nhật thương hiệu thành công.")
            : ApiResponse<bool>.Fail("Không thể cập nhật lúc này.", 500, "SERVER_ERROR");
    }
}

public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, ApiResponse<bool>>
{
    private readonly IBrandRepository _repo;
    public DeleteBrandHandler(IBrandRepository repo) => _repo = repo;

    public async Task<ApiResponse<bool>> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return ApiResponse<bool>.Fail("Không tìm thấy thương hiệu.", 404, "BRAND_NOT_FOUND");

        try
        {
            var result = await _repo.DeleteAsync(request.Id, cancellationToken);
            return result 
                ? ApiResponse<bool>.Succeed(true, "Đã xóa thương hiệu.")
                : ApiResponse<bool>.Fail("Không thể xóa lúc này.", 500, "SERVER_ERROR");
        }
        catch (Exception)
        {
            // Tránh xóa cứng ném lỗi FK constraint khi đang có sản phẩm gắn liền
            existing.IsActive = false;
            await _repo.UpdateAsync(existing, cancellationToken);
            return ApiResponse<bool>.Succeed(true, "Thương hiệu đang có sản phẩm, đã chuyển sang trạng thái Ẩn thay vì xóa.");
        }
    }
}
