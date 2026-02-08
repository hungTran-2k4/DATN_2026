using System.Data;
using AutoMapper;
using DATN.DatabaseSpecific;
using DATN.EntityClasses;
using DATN.FactoryClasses;
using DATN.HelperClasses;
using DATN.Linq;
using MyProject.Application.Interfaces.Publishers;
using MyProject.Domain.Entities.Stores;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using Microsoft.Extensions.Logging;
using SD.LLBLGen.Pro.LinqSupportClasses;

namespace MyProject.Infrastructure.Persistence.Repositories.Publishers
{
    public class PublisherRepository : IPublisherRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<PublisherRepository> _logger;

        public PublisherRepository(IMapper mapper, ILogger<PublisherRepository> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Publisher> CreateAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            using (var adapter = new DataAccessAdapter())
            {
                if (publisher == null) throw new ArgumentNullException(nameof(publisher));

                var publisherExists = await GetByIdAsync(publisher.Id, cancellationToken);
                if(publisherExists != null)
                {
                    throw new Exception("Failed to create publisher");
                }

                var newPublisherEntity = new PublisherEntity()
                {
                    Id = Guid.NewGuid(),
                    Name = publisher.Name,
                };

                var entity = await adapter.SaveEntityAsync(newPublisherEntity, cancellationToken);
                var result = _mapper.Map<Publisher>(entity);
                return result;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using (var adapter = new DataAccessAdapter())
            {
                var existsPublisher = await adapter.FetchFirstAsync(
                    new QueryFactory().Create<PublisherEntity>()
                        .Where(PublisherFields.Id == id),
                    cancellationToken);
                if (existsPublisher == null)
                {
                    _logger.LogWarning("Publisher not found for deletion: {PublisherId}", id);
                    return false;
                }

                var result = await adapter.DeleteEntityAsync(existsPublisher, cancellationToken);
                
                return result;
            }
        }

        public async Task<IEnumerable<Publisher>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            using (var adapter = new DataAccessAdapter())
            {
                var qf = new QueryFactory();
                var query = qf.Create<PublisherEntity>()
                    .OrderBy(PublisherFields.Name.Ascending())
                    .Page(pageIndex, pageSize);
                var publisherEntities = await adapter.FetchQueryAsync(query, cancellationToken);
                var result = _mapper.Map<IEnumerable<Publisher>>(publisherEntities);
                return result;
            }
        }

        public async Task<Publisher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using (var adapter = new DataAccessAdapter())
            {
                var linqAdapter = new LinqMetaData(adapter);
                var entity = await linqAdapter.Publisher
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                if(entity == null)
                {
                    return null;
                }

                var result = _mapper.Map<Publisher>(entity);
                return result;
            }
        }

        public async Task<bool> UpdateAsync(Publisher publisher, CancellationToken cancellationToken = default)
        {
            //cập nhật nhiều bản ghi
            using(var adapter = new DataAccessAdapter())
            {
                var entity = new PublisherEntity();
                entity.Description = "Updated Description";
                entity.Fields[nameof(PublisherEntity.Description)].IsChanged = true;
                var result = await adapter.UpdateEntitiesDirectlyAsync(
                    entity,
                    new RelationPredicateBucket(PublisherFields.Name == "abc"),
                    cancellationToken
                );

                return result > 0;
            }

        }
    }
}
