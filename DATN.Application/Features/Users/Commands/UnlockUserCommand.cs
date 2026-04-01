using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class UnlockUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public UnlockUserCommand(Guid userId)
    {
        UserId = userId;
    }
}
