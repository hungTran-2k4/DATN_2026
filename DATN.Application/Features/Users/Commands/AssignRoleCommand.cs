using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class AssignRoleCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public AssignRoleCommand(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
