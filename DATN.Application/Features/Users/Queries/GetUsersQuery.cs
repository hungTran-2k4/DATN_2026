using MediatR;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Users.Queries;

public class GetUsersQuery : IRequest<IEnumerable<UserDto>>
{
}
