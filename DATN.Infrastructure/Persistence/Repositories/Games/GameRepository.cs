using System.Data;
using AutoMapper;
using DATN.DatabaseSpecific;
using DATN.EntityClasses;
using DATN.FactoryClasses;
using DATN.HelperClasses;
using DATN.Linq;
using MyProject.Application.Interfaces.Games;
using MyProject.Domain.Entities.Stores;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using Microsoft.Extensions.Logging;
using SD.LLBLGen.Pro.LinqSupportClasses;
using Microsoft.Extensions.Configuration;

namespace MyProject.Infrastructure.Persistence.Repositories.Games;

public class GameRepository : IGameRepository
{
    private DataAccessAdapter _scopedAdapter;
    private readonly IMapper _mapper;
    private readonly ILogger<GameRepository> _logger;

    public GameRepository(DataAccessAdapter scopedAdapter, IMapper mapper, ILogger<GameRepository> logger, IConfiguration configuration)
    {
        _scopedAdapter = scopedAdapter;
        _mapper = mapper;
        _logger = logger;
       
    }

    public async Task<Game> CreateAsync(Game game, CancellationToken cancellationToken = default)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));

       
        await _scopedAdapter.StartTransactionAsync(IsolationLevel.ReadCommitted, "Create game");
        try
        {
            var gameExists = await GetByIdAsync(game.Id, cancellationToken);
            if (gameExists != null)
            {
                throw new Exception("Game already exists");
            }

            var entity = _mapper.Map<GameEntity>(game);
            entity.IsNew = true;
            var result = await _scopedAdapter.SaveEntityAsync(entity, cancellationToken);
            if (!result)
            {
                throw new Exception("Failed to create game");
            }

            _logger.LogInformation("Game created successfully: {GameId}", entity.Id);
            await _scopedAdapter.CommitAsync(cancellationToken);
            return _mapper.Map<Game>(entity);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating game");
            _scopedAdapter.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var adapter = new DataAccessAdapter();
        var linq = new LinqMetaData(adapter);
        var gameToDelete = await linq.Game.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (gameToDelete == null)
        {
            return false;
        }

        var result = await _scopedAdapter.DeleteEntityAsync(gameToDelete, cancellationToken);
        return result;
    }

    public async Task<IEnumerable<Game>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var qf = new QueryFactory();
            var query = qf.Create<GameEntity>()
                .WithPath(GameEntity.PrefetchPathPublisher)
                .OrderBy(GameFields.Title.Ascending());
            var entities = await _scopedAdapter.FetchQueryAsync(query, cancellationToken);
            return _mapper.Map<IEnumerable<Game>>(entities);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting all games");
            throw;
        }
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var qf = new QueryFactory();
            var query = qf.Create<GameEntity>()
                .Where(GameFields.Id == id)
                .WithPath(GameEntity.PrefetchPathPublisher);

            var entity = await _scopedAdapter.FetchFirstAsync(query, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("Game not found: {GameId}", id);
                return null;
            }

            return _mapper.Map<Game>(entity);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting game by ID: {GameId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Game>> GetByPublisherIdAsync(Guid publisherId, CancellationToken cancellationToken = default)
    {
        using var adapter = new DataAccessAdapter();
        var qf = new QueryFactory();
        var query = qf.Create<GameEntity>()
            .Where(GameFields.PublisherId == publisherId)
            .OrderBy(GameFields.ReleaseDate.Descending());
        var entities = await adapter.FetchQueryAsync(query, cancellationToken);

        return _mapper.Map<IEnumerable<Game>>(entities);
    }

    public async Task<bool> UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        try
        {
            using var adapter = new DataAccessAdapter();

            var gameExists = await GetByIdAsync(game.Id, cancellationToken);
            if (gameExists == null)
            {
                throw new Exception("Game not found for update");
            }

            var entity = _mapper.Map<GameEntity>(game);
            entity.IsNew = false;

            var result = await adapter.SaveEntityAsync(entity, cancellationToken);
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }
}