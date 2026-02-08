using AutoMapper;
using MyProject.Application.Interfaces.Auth;
using MyProject.Domain.Entities.Identity;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System.Data;
using DATN.DatabaseSpecific;
using DATN.EntityClasses;
using DATN.FactoryClasses;
using DATN.HelperClasses;
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Persistence.Repositories.Auth;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DataAccessAdapter _adapter;
    private readonly IMapper _mapper;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(DataAccessAdapter adapter, IMapper mapper, ILogger<RefreshTokenRepository> logger)
    {
        _adapter = adapter;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<RefreshTokenEntity>(refreshToken);
        entity.IsNew = true;
        await _adapter.SaveEntityAsync(entity, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.RefreshToken.Where(RefreshTokenFields.TokenHash == tokenHash);
        
        // Use FetchQuery to check for duplicates
        var tokens = await _adapter.FetchQueryAsync(query, cancellationToken) as EntityCollection<RefreshTokenEntity>;
        
        if (tokens == null || tokens.Count == 0)
        {
            _logger.LogWarning("GetByTokenHashAsync: Token not found. Hash: {TokenHash}", tokenHash);
            return null;
        }

        if (tokens.Count > 1)
        {
            _logger.LogError("CRITICAL: Found {Count} tokens with the same hash! Hash: {TokenHash}", tokens.Count, tokenHash);
            foreach(var t in tokens)
            {
                _logger.LogWarning("Duplicate Token Id: {Id}, Revoked: {Revoked}", t.Id, t.Revoked);
            }
        }

        var entity = tokens[0];

        _logger.LogInformation("GetByTokenHashAsync: Found Token {Id}. Revoked: {Revoked}, RevokedAt: {RevokedAt}", entity.Id, entity.Revoked, entity.RevokedAt);

        return _mapper.Map<RefreshToken>(entity);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.RefreshToken.Where(RefreshTokenFields.Id == refreshToken.Id);
        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);
        
        if (entity != null)
        {
            // Use AutoMapper to copy all properties from Domain to Entity
            _mapper.Map(refreshToken, entity);
            
            // Ensure IsNew is false for update
            entity.IsNew = false;

            _logger.LogInformation("Updating RefreshToken {Id}. Revoked: {Revoked}, RevokedAt: {RevokedAt}", entity.Id, entity.Revoked, entity.RevokedAt);

            await _adapter.SaveEntityAsync(entity, cancellationToken);
        }
        else
        {
            _logger.LogWarning("RefreshToken {Id} not found for update.", refreshToken.Id);
        }
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.RefreshToken.Where(RefreshTokenFields.UserId == userId);
        
        var tokens = await _adapter.FetchQueryAsync(query, cancellationToken) as EntityCollection<RefreshTokenEntity>;
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.Revoked = true;
            token.IsNew = false;
            await _adapter.SaveEntityAsync(token, cancellationToken);
        }
    }

    public async Task RemoveExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var bucket = new RelationPredicateBucket();
        bucket.PredicateExpression.Add(RefreshTokenFields.ExpiresAt < DateTime.UtcNow);
        await _adapter.DeleteEntitiesDirectlyAsync(typeof(RefreshTokenEntity), bucket, cancellationToken);
    }
}
