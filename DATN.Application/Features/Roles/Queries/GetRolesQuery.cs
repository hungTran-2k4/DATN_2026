using MediatR;
using DATN.Application.DTOs.Auth;
using System.Collections.Generic;

namespace DATN.Application.Features.Roles.Queries;

public class GetRolesQuery : IRequest<IEnumerable<RoleDto>>
{
}
