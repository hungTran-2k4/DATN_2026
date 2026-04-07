using MediatR;
using DATN.Application.Common.Models;

namespace DATN.Application.Features.Users.Commands;

public class CreateUserCommand : IRequest<ApiResponse<Guid>>
{
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Password { get; set; }
    public string? PhoneNumber { get; set; }
    public List<Guid>? RoleIds { get; set; }

    public CreateUserCommand(string email, string fullName, string password, string? phoneNumber, List<Guid>? roleIds)
    {
        Email = email;
        FullName = fullName;
        Password = password;
        PhoneNumber = phoneNumber;
        RoleIds = roleIds;
    }
}
