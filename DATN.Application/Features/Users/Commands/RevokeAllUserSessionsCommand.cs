using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class RevokeAllUserSessionsCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public RevokeAllUserSessionsCommand(Guid userId)
    {
        UserId = userId;
    }
}
