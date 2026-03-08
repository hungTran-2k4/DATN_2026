using DATN.Domain.Entities.Identity;

namespace DATN.Domain.Interfaces;

/// <summary>
/// Repository interface cho RefreshToken operations
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveExpiredTokensAsync(CancellationToken cancellationToken = default);
}
