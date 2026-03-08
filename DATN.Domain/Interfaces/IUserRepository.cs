using DATN.Domain.Entities.Identity;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Interface cho User Repository
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Lấy user theo Id
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user theo Email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo user mới
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật user
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách roles của user
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gán role cho user
    /// </summary>
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lấy danh sách tất cả users
    /// </summary>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tất cả roles của user
    /// </summary>
    Task ClearUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tăng số lần đăng nhập sai. Nếu >= maxAttempts, set LockoutEnd.
    /// </summary>
    Task IncrementFailedLoginAsync(Guid userId, int maxAttempts = 3, int lockoutMinutes = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset số lần đăng nhập sai về 0 và xóa LockoutEnd
    /// </summary>
    Task ResetFailedLoginAsync(Guid userId, CancellationToken cancellationToken = default);
}
