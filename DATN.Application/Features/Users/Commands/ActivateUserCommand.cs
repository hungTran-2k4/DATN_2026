using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class ActivateUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public ActivateUserCommand(Guid userId)
    {
        UserId = userId;
    }
}
