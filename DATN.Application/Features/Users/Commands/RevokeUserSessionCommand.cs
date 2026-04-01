using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class RevokeUserSessionCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }

    public RevokeUserSessionCommand(Guid userId, Guid sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }
}
