using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class UpdateUserRolesHandler : IRequestHandler<UpdateUserRolesCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UpdateUserRolesHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<bool> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        // 2. Validate all Roles exist? (Optional but recommended)
        // For performance, we might skip checking each ID individually if trusted, 
        // but for safety, better to verify or just let FK constraints fail if db enforce it.
        // Or we can simple assuming frontend sent valid IDs.

        // 3. Clear existing roles
        await _userRepository.ClearUserRolesAsync(user.Id, cancellationToken);

        // 4. Assign new roles
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                // We could verify role existence here or inside AssignRoleAsync if needed.
                // Assuming IDs are valid
                await _userRepository.AssignRoleAsync(user.Id, roleId, cancellationToken);
            }
        }
        
        return true;
    }
}
