using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Auth;
using DATN.Domain.Common.Models;
using System.Collections.Generic;

namespace DATN.Application.Features.Users.Queries;

public class GetUsersQuery : PagedRequest, IRequest<PagedResponse<IEnumerable<UserDto>>>
{
    public GetUsersQuery(string? search = null, FilterDescriptor? filter = null, int page = 1, int pageSize = 10)
        : base(search, filter, page, pageSize)
    {
    }
}
