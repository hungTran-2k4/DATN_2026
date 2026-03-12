using AutoMapper;
using Microsoft.Extensions.Configuration;
using DATN.Application.Interfaces.Services;
using DATN.Domain.Interfaces;
using DATN.Domain.Entities.Identity;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;
using SD.LLBLGen.Pro.QuerySpec.Adapter;
using System.Data;
using DATN_2026.DatabaseSpecific;
using DATN_2026.EntityClasses;
using DATN_2026.FactoryClasses;
using DATN_2026.HelperClasses;

namespace DATN.Infrastructure.Persistence.Repositories.Users;

/// <summary>
/// Implementation của IUserRepository sử dụng LLBLGen
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IMapper _mapper;
    private readonly DataAccessAdapter _adapter;

    public UserRepository(IMapper mapper, DataAccessAdapter adapter)
    {
        _mapper = mapper;
        _adapter = adapter;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Id == id);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (entity == null)
            return null;

        return _mapper.Map<User>(entity);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Email == email);

        var entity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (entity == null)
            return null;

        return _mapper.Map<User>(entity);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Email == email);

        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);

        return entities.Count > 0;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.OrderBy(UserFields.Username.Ascending())
            // Prefetch UserRoles and Role to avoid N+1
            .WithPath(UserEntity.PrefetchPathUserRoles
                .WithSubPath(UserRoleEntity.PrefetchPathRole));
        
        var entities = await _adapter.FetchQueryAsync(query, cancellationToken);
        
        return _mapper.Map<IEnumerable<User>>(entities);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var userEntity = new UserEntity
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Email,
            PasswordHash = user.PasswordHash,
            Status = user.IsActive ? "active" : "inactive",
            CreatedAt = user.CreatedAt
        };
        userEntity.IsNew = true;

        var result = await _adapter.SaveEntityAsync(userEntity, cancellationToken);
        if (!result)
        {
            throw new Exception("Failed to create user");
        }

        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Id == user.Id);

        var userEntity = await _adapter.FetchFirstAsync(query, cancellationToken);

        if (userEntity == null)
            throw new InvalidOperationException($"User with id {user.Id} not found");

        userEntity.Email = user.Email;
        userEntity.PasswordHash = user.PasswordHash;
        userEntity.Status = user.IsActive ? "active" : "inactive";
        userEntity.UpdatedAt = DateTime.Now;
        userEntity.IsNew = false;

        await _adapter.SaveEntityAsync(userEntity, cancellationToken);

        return user;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Fetch UserRoles
        var qf = new QueryFactory();
        var query = qf.Create()
            .Select(RoleFields.Name)
            .From(qf.Role.InnerJoin(qf.UserRole).On(RoleFields.Id == UserRoleFields.RoleId))
            .Where(UserRoleFields.UserId == userId);

        var result = await _adapter.FetchQueryAsync(query, cancellationToken);

        return result.Cast<object[]>().Select(row => row[0]?.ToString() ?? string.Empty).ToList();
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = new UserRoleEntity
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.Now
        };
        userRole.IsNew = true;

        await _adapter.SaveEntityAsync(userRole, cancellationToken);
    }

    public async Task ClearUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var bucket = new RelationPredicateBucket();
        bucket.PredicateExpression.Add(UserRoleFields.UserId == userId);
        await _adapter.DeleteEntitiesDirectlyAsync(typeof(UserRoleEntity), bucket, cancellationToken);
    }

    public async Task IncrementFailedLoginAsync(Guid userId, int maxAttempts = 3, int lockoutMinutes = 5, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Id == userId);

        var userEntity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (userEntity == null) return;

        userEntity.FailedLoginCount += 1;

        if (userEntity.FailedLoginCount >= maxAttempts)
        {
            userEntity.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }

        userEntity.IsNew = false;
        await _adapter.SaveEntityAsync(userEntity, cancellationToken);
    }

    public async Task ResetFailedLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var qf = new QueryFactory();
        var query = qf.User.Where(UserFields.Id == userId);

        var userEntity = await _adapter.FetchFirstAsync(query, cancellationToken);
        if (userEntity == null) return;

        userEntity.FailedLoginCount = 0;
        userEntity.LockoutEnd = null;
        userEntity.IsNew = false;
        await _adapter.SaveEntityAsync(userEntity, cancellationToken);
    }
}
