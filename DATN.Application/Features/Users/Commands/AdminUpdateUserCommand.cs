using MediatR;

namespace DATN.Application.Features.Users.Commands;

public class AdminUpdateUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }

    public AdminUpdateUserCommand(Guid userId, string? fullName)
    {
        UserId = userId;
        FullName = fullName;
    }
}
