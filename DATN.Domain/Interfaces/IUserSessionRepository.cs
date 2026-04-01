using DATN.Domain.Entities.Audit;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Repository interface cho User Session operations
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>
    /// Lấy danh sách sessions active của user
    /// </summary>
    Task<IEnumerable<UserSessionEntry>> GetActiveSessionsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke 1 session cụ thể
    /// </summary>
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke tất cả sessions của user
    /// </summary>
    Task RevokeAllSessionsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo session mới khi user login
    /// </summary>
    Task<UserSessionEntry> CreateSessionAsync(Guid userId, string? ipAddress, string? userAgent,
                                               CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật lastActivityAt
    /// </summary>
    Task UpdateLastActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
