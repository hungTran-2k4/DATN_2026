using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;

namespace DATN.Application.Features.Users.Queries;

public class GetUserLoginHistoryQuery : IRequest<PagedResponse<IEnumerable<LoginAttemptDto>>>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public GetUserLoginHistoryQuery(Guid userId, int page = 1, int pageSize = 20)
    {
        UserId = userId;
        Page = page;
        PageSize = pageSize;
    }
}
