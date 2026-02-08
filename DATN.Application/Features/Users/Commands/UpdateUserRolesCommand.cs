using MediatR;

namespace MyProject.Application.Features.Users.Commands;

public class UpdateUserRolesCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();

    public UpdateUserRolesCommand(Guid userId, List<Guid> roleIds)
    {
        UserId = userId;
        RoleIds = roleIds;
    }
}
