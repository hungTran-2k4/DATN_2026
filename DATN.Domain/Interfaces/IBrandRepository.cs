using DATN.Domain.Entities.Products;

namespace DATN.Domain.Interfaces;

public interface IBrandRepository
{
    Task<(IEnumerable<Brand> Items, int Total)> GetPagedAsync(
        string? search = null, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Brand>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    
    Task<Brand> AddAsync(Brand brand, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Brand brand, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
