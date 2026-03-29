using AutoMapper;
using DATN.Domain.Interfaces;
using DATN.Domain.Entities.Identity;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;

namespace DATN.Infrastructure.Persistence.Repositories.Auth;

/// <summary>
/// Implementation của IPasswordResetTokenRepository sử dụng LLBLGen
/// </summary>
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly DataAccessAdapter _adapter;

    public PasswordResetTokenRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        var entity = new PasswordResetTokenEntity
        {
            Id = token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            IsUsed = token.IsUsed,
            CreatedAt = token.CreatedAt,
            IpAddress = token.IpAddress
        };
        entity.IsNew = true;

        var result = await _adapter.SaveEntityAsync(entity, cancellationToken);
        if (!result)
        {
            throw new Exception("Failed to create password reset token");
        }

        return token;
    }

    public async Task<PasswordResetToken?> GetValidTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.PasswordResetToken
            .Where(PasswordResetTokenFields.TokenHash == tokenHash
                & PasswordResetTokenFields.IsUsed == false
                & PasswordResetTokenFields.ExpiresAt > DateTime.UtcNow);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (entity == null)
            return null;

        return new PasswordResetToken
        {
            Id = entity.Id,
            UserId = entity.UserId,
            TokenHash = entity.TokenHash,
            ExpiresAt = entity.ExpiresAt,
            IsUsed = entity.IsUsed,
            UsedAt = entity.UsedAt,
            CreatedAt = entity.CreatedAt,
            IpAddress = entity.IpAddress
        };
    }

    public async Task MarkAsUsedAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.PasswordResetToken.Where(PasswordResetTokenFields.Id == tokenId);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return;

        entity.IsUsed = true;
        entity.UsedAt = DateTime.UtcNow;
        entity.IsNew = false;

        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }

    public async Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Update tất cả token chưa dùng của user thành is_used = true
        var entity = new PasswordResetTokenEntity { IsUsed = true, UsedAt = DateTime.UtcNow };
        var bucket = new RelationPredicateBucket();
        bucket.PredicateExpression.Add(PasswordResetTokenFields.UserId == userId);
        bucket.PredicateExpression.AddWithAnd(PasswordResetTokenFields.IsUsed == false);

        await _adapter.UpdateEntitiesDirectlyAsync(entity, bucket, cancellationToken);
    }

    public async Task<int> CountTodayByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var todayStart = DateTime.UtcNow.Date;

        var qf = new QueryFactory();
        var query = qf.PasswordResetToken
            .Where(PasswordResetTokenFields.UserId == userId
                & PasswordResetTokenFields.CreatedAt >= todayStart);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        return entities.Count;
    }
}
