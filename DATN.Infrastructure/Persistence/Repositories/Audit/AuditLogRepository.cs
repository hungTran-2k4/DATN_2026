using System.Text.Json;
using DATN.Domain.Entities.Audit;
using DATN.Domain.Interfaces;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using SD.LLBLGen.Pro.ORMSupportClasses;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;

namespace DATN.Infrastructure.Persistence.Repositories.Audit;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly DataAccessAdapter _adapter;

    public AuditLogRepository(DataAccessAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task LogAsync(Guid userId, string action, string? targetType = null, Guid? targetId = null,
                                object? metadata = null, string? ipAddress = null, string? userAgent = null,
                                CancellationToken cancellationToken = default)
    {
        var entity = new UserAuditLogEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        entity.IsNew = true;

        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }

    public async Task<(IEnumerable<AuditLogEntry> items, int totalCount)> GetPagedByUserAsync(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();

        // Count
        var countQuery = qf.UserAuditLog
            .Select(Functions.CountRow())
            .Where(UserAuditLogFields.UserId == userId);
        int totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        // Data
        var query = qf.UserAuditLog
            .Where(UserAuditLogFields.UserId == userId)
            .OrderBy(UserAuditLogFields.CreatedAt.Descending())
            .Offset((page - 1) * pageSize)
            .Limit(pageSize);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);

        var items = entities.Cast<UserAuditLogEntity>().Select(e => new AuditLogEntry
        {
            Id = e.Id,
            UserId = e.UserId.GetValueOrDefault(),
            Action = e.Action,
            TargetType = e.TargetType,
            TargetId = e.TargetId,
            Metadata = e.Metadata,
            IpAddress = e.IpAddress,
            UserAgent = e.UserAgent,
            CreatedAt = e.CreatedAt
        }).ToList();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<LoginAttemptEntry> items, int totalCount)> GetLoginAttemptsAsync(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();

        var countQuery = qf.LoginAttempt
            .Select(Functions.CountRow())
            .Where(LoginAttemptFields.UserId == userId);
        int totalCount = await _adapter.FetchScalarAsync<int>(countQuery, cancellationToken);

        var query = qf.LoginAttempt
            .Where(LoginAttemptFields.UserId == userId)
            .OrderBy(LoginAttemptFields.AttemptedAt.Descending())
            .Offset((page - 1) * pageSize)
            .Limit(pageSize);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);

        var items = entities.Cast<LoginAttemptEntity>().Select(e => new LoginAttemptEntry
        {
            Id = e.Id,
            UserId = e.UserId,
            Email = e.Email,
            IpAddress = e.IpAddress,
            Success = e.Success,
            AttemptedAt = e.AttemptedAt
        }).ToList();

        return (items, totalCount);
    }

    public async Task LogLoginAttemptAsync(Guid? userId, string email, string? ipAddress, bool success,
                                            CancellationToken cancellationToken = default)
    {
        var entity = new LoginAttemptEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            Success = success,
            AttemptedAt = DateTime.UtcNow
        };
        entity.IsNew = true;

        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }
}
