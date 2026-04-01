using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class DeactivateUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? Reason { get; set; }

    public DeactivateUserCommand(Guid userId, string? reason = null)
    {
        UserId = userId;
        Reason = reason;
    }
}
