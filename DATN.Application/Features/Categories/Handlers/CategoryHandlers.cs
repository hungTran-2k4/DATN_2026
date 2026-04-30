using DATN.Application.Common.Models;
using DATN.Application.DTOs.Categories;
using DATN.Application.Features.Categories.Commands;
using DATN.Application.Features.Categories.Queries;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Entities.Categories;
using DATN.Domain.Interfaces;
using MediatR;
using System.Text.RegularExpressions;
using DATN.Application.Common;

namespace DATN.Application.Features.Categories.Handlers;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, ApiResponse<IEnumerable<CategoryDto>>>
{
    private readonly ICategoryRepository _repo;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public GetCategoriesHandler(ICategoryRepository repo, ICacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, ApiResponse<bool>>
{
    private readonly ICategoryRepository _repo;
    private readonly ICacheService _cache;

    public UpdateCategoryHandler(ICategoryRepository repo, ICacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
            return ApiResponse<bool>.Fail("Không tìm thấy danh mục.", 404, "CATEGORY_NOT_FOUND");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug.Trim().ToLower();

        if (await _repo.SlugExistsAsync(slug, request.Id, cancellationToken))
            return ApiResponse<bool>.Fail("Slug đã tồn tại.", 400, "SLUG_EXISTS");

        existing.Name = request.Name;
        existing.Slug = slug;
        existing.IconUrl = request.IconUrl;
        existing.ParentId = request.ParentId;
        existing.DisplayOrder = request.DisplayOrder;
        existing.IsActive = request.IsActive;

        var updated = await _repo.UpdateAsync(existing, cancellationToken);
        if (!updated)
            return ApiResponse<bool>.Fail("Cập nhật danh mục thất bại.", 400, "CATEGORY_UPDATE_FAILED");

        _cache.RemoveByPrefix("categories:");
        return ApiResponse<bool>.Succeed(true, "Cập nhật danh mục thành công.");
    }

    private static string GenerateSlug(string name) =>
        SlugHelper.GenerateSlug(name);
}

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var key = $"categories:tree:activeOnly={request.ActiveOnly}";
        return await _cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var all = await _repo.GetAllAsync(ct);
                if (request.ActiveOnly)
                    all = all.Where(c => c.IsActive == true);

                var tree = BuildTree(all.ToList(), null);
                return ApiResponse<IEnumerable<CategoryDto>>.Succeed(tree);
            },
            Ttl,
            cancellationToken);
    }

    private static List<CategoryDto> BuildTree(List<Category> all, Guid? parentId)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IconUrl = c.IconUrl,
                ParentId = c.ParentId,
                IsActive = c.IsActive ?? true,
                DisplayOrder = c.DisplayOrder ?? 0,
                Children = BuildTree(all, c.Id)
            })
            .ToList();
    }
}

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, ApiResponse<CategoryDto>>
{
    private readonly ICategoryRepository _repo;
    public GetCategoryByIdHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<ApiResponse<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var cat = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (cat == null) return ApiResponse<CategoryDto>.Fail("Không tìm thấy danh mục.", 404, "CATEGORY_NOT_FOUND");
        return ApiResponse<CategoryDto>.Succeed(new CategoryDto
        {
            Id = cat.Id, Name = cat.Name, Slug = cat.Slug,
            IconUrl = cat.IconUrl, ParentId = cat.ParentId,
            IsActive = cat.IsActive ?? true, DisplayOrder = cat.DisplayOrder ?? 0
        });
    }
}

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, ApiResponse<CategoryDto>>
{
    private readonly ICategoryRepository _repo;
    private readonly ICacheService _cache;

    public CreateCategoryHandler(ICategoryRepository repo, ICacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<ApiResponse<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug.Trim().ToLower();

        if (await _repo.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            return ApiResponse<CategoryDto>.Fail("Slug đã tồn tại.", 400, "SLUG_EXISTS");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            IconUrl = request.IconUrl,
            ParentId = request.ParentId,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(category, cancellationToken);
        _cache.RemoveByPrefix("categories:");
        return ApiResponse<CategoryDto>.Succeed(new CategoryDto
        {
            Id = created.Id, Name = created.Name, Slug = created.Slug,
            IconUrl = created.IconUrl, ParentId = created.ParentId,
            IsActive = created.IsActive ?? true, DisplayOrder = created.DisplayOrder ?? 0
        }, "Tạo danh mục thành công.", 201);
    }

    private static string GenerateSlug(string name) =>
        SlugHelper.GenerateSlug(name);
}

public class DeactivateCategoryHandler : IRequestHandler<DeactivateCategoryCommand, ApiResponse<bool>>
{
    private readonly ICategoryRepository _repo;
    private readonly ICacheService _cache;

    public DeactivateCategoryHandler(ICategoryRepository repo, ICacheService cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.DeactivateAsync(request.Id, cancellationToken);
        if (result) _cache.RemoveByPrefix("categories:");
        return result
            ? ApiResponse<bool>.Succeed(true, "Đã ẩn danh mục.")
            : ApiResponse<bool>.Fail("Không tìm thấy danh mục hoặc danh mục đang có sản phẩm.", 400, "CANNOT_DEACTIVATE");
    }
}
