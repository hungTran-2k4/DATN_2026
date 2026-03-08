using DATN.Domain.Entities.Identity;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Interface cho Role Repository
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Lấy role theo Id
    /// </summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy role theo tên
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả roles
    /// </summary>
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo role mới
    /// </summary>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);
}
