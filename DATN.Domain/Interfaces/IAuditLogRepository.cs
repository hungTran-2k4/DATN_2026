using DATN.Domain.Entities.Audit;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Repository interface cho Audit Log operations
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Ghi một audit log entry
    /// </summary>
    Task LogAsync(Guid userId, string action, string? targetType = null, Guid? targetId = null,
                  object? metadata = null, string? ipAddress = null, string? userAgent = null,
                  CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách audit logs theo userId có phân trang
    /// </summary>
    Task<(IEnumerable<AuditLogEntry> items, int totalCount)> GetPagedByUserAsync(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách login attempts theo userId có phân trang
    /// </summary>
    Task<(IEnumerable<LoginAttemptEntry> items, int totalCount)> GetLoginAttemptsAsync(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi login attempt
    /// </summary>
    Task LogLoginAttemptAsync(Guid? userId, string email, string? ipAddress, bool success,
                              CancellationToken cancellationToken = default);
}
