using MediatR;
using MyProject.Application.Models.Auth;

namespace MyProject.Application.Features.Users.Queries;

public class GetUsersQuery : IRequest<IEnumerable<UserDto>>
{
}
