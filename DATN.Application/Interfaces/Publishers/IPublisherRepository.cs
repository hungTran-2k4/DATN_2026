using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyProject.Domain.Entities.Stores;

namespace MyProject.Application.Interfaces.Publishers
{
    public interface IPublisherRepository
    {
        Task<Publisher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Publisher>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
        Task<Publisher> CreateAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Publisher publisher, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
