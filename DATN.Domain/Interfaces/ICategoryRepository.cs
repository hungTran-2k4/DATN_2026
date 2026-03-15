using DATN.Domain.Entities.Categories;

namespace DATN.Domain.Interfaces;

public interface ICategoryRepository
{
    /// <summary>Lấy danh sách categories có phân trang, lọc, search</summary>
    Task<(IEnumerable<Category> Items, int Total)> GetPagedAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>Lấy toàn bộ categories dạng flat list (có ParentId để build tree ở Application layer)</summary>
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>Xóa mềm: set IsActive = false. Không xóa thực nếu còn products hoặc sub-categories</summary>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra slug đã tồn tại chưa (để validate unique)</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
