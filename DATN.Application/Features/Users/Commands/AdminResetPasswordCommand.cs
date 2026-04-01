using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class AdminResetPasswordCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public AdminResetPasswordCommand(Guid userId)
    {
        UserId = userId;
    }
}
