using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class LockUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? Reason { get; set; }

    public LockUserCommand(Guid userId, string? reason = null)
    {
        UserId = userId;
        Reason = reason;
    }
}
