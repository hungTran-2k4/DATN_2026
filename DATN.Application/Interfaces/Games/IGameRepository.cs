using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain.Entities.Stores;

namespace MyProject.Application.Interfaces.Games
{
    public interface IGameRepository
    {
        Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Game>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Game>> GetByPublisherIdAsync(Guid publisherId, CancellationToken cancellationToken = default);
        Task<Game> CreateAsync(Game game, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Game game, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}