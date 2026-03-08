using MediatR;
using DATN.Application.Features.Users.Commands;
using DATN.Domain.Interfaces;

namespace DATN.Application.Features.Users.Handlers;

public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public AssignRoleHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<bool> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return false;

        // Check if role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null) return false;

        // Check if user already has role? (Repo might throw or handle it)
        // For simplicity, we just try assign. 
        // Ideal: Repository should handle duplicate check or use "TryAssign".
        // Here we assume it's safe or will throw if duplicate constraint violates.
        
        await _userRepository.AssignRoleAsync(user.Id, role.Id, cancellationToken);
        
        return true;
    }
}
