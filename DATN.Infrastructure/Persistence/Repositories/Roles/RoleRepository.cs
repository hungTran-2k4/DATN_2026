using AutoMapper;
using DATN.DatabaseSpecific;
using DATN.EntityClasses;
using DATN.FactoryClasses;
using DATN.HelperClasses;
using Microsoft.Extensions.Configuration;
using MyProject.Application.Interfaces.Services;
using MyProject.Application.Interfaces.Roles;
using MyProject.Domain.Entities.Identity;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;

namespace MyProject.Infrastructure.Persistence.Repositories.Roles;

/// <summary>
/// Implementation của IRoleRepository sử dụng LLBLGen và AutoMapper
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IMapper _mapper;
    private readonly DataAccessAdapter _adapter;

    public RoleRepository(IMapper mapper, DataAccessAdapter adapter)
    {
        _mapper = mapper;
        _adapter = adapter;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Role.Where(RoleFields.Id == id);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (entity == null)
            return null;

        return _mapper.Map<Role>(entity);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Role.Where(RoleFields.Name == name);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (entity == null)
            return null;

        return _mapper.Map<Role>(entity);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.Role.OrderBy(RoleFields.Name.Ascending());

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);

        return _mapper.Map<IEnumerable<Role>>(entities);
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var roleEntity = _mapper.Map<RoleEntity>(role);
        roleEntity.IsNew = true;

        var result = await _adapter.SaveEntityAsync(roleEntity, cancellationToken);
        if (!result)
        {
            throw new Exception("Failed to create role");
        }

        return role;
    }
}
