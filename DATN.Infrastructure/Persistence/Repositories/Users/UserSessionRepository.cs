using DATN.Domain.Entities.Audit;
using DATN.Domain.Interfaces;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using SD.LLBLGen.Pro.ORMSupportClasses;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;

namespace DATN.Infrastructure.Persistence.Repositories.Users;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly DataAccessAdapter _adapter;

    public UserSessionRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<IEnumerable<UserSessionEntry>> GetActiveSessionsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.UserSession
            .Where(UserSessionFields.UserId == userId & UserSessionFields.RevokedAt == DBNull.Value)
            .OrderBy(UserSessionFields.LastActivityAt.Descending());

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);

        return entities.Cast<UserSessionEntity>().Select(e => new UserSessionEntry
        {
            Id = e.Id,
            UserId = e.UserId,
            IpAddress = e.IpAddress,
            UserAgent = e.UserAgent,
            CreatedAt = e.CreatedAt,
            LastActivityAt = e.LastActivityAt,
            RevokedAt = e.RevokedAt
        }).ToList();
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.UserSession.Where(UserSessionFields.Id == sessionId);
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return;

        entity.RevokedAt = DateTime.UtcNow;
        entity.IsNew = false;
        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }

    public async Task RevokeAllSessionsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.UserSession
            .Where(UserSessionFields.UserId == userId & UserSessionFields.RevokedAt == DBNull.Value);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        foreach (UserSessionEntity entity in entities)
        {
            entity.RevokedAt = DateTime.UtcNow;
            entity.IsNew = false;
        }
        if (entities.Count > 0)
        {
            await _adapter.SaveEntityCollectionAsync(entities, cancellationToken);
        }
    }

    public async Task<UserSessionEntry> CreateSessionAsync(Guid userId, string? ipAddress, string? userAgent,
                                                            CancellationToken cancellationToken = default)
    {
        var entity = new UserSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };
        entity.IsNew = true;

        await _adapter.SaveEntityAsync(entity, cancellationToken);

        return new UserSessionEntry
        {
            Id = entity.Id,
            UserId = entity.UserId,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CreatedAt = entity.CreatedAt,
            LastActivityAt = entity.LastActivityAt
        };
    }

    public async Task UpdateLastActivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.UserSession.Where(UserSessionFields.Id == sessionId);
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (entity == null) return;

        entity.LastActivityAt = DateTime.UtcNow;
        entity.IsNew = false;
        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }
}
