using DATN.Domain.Entities.Identity;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Repository cho Password Reset Token
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Tạo token mới
    /// </summary>
    Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy token hợp lệ (chưa dùng, chưa hết hạn) theo token hash
    /// </summary>
    Task<PasswordResetToken?> GetValidTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu token đã sử dụng
    /// </summary>
    Task MarkAsUsedAsync(Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vô hiệu hóa tất cả token cũ của user (khi tạo token mới)
    /// </summary>
    Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số token đã tạo hôm nay cho user (rate limit)
    /// </summary>
    Task<int> CountTodayByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
