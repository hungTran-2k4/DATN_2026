using MediatR;
using DATN.Application.DTOs.Users;

namespace DATN.Application.Features.Users.Queries;

public class GetUserSessionsQuery : IRequest<IEnumerable<UserSessionDto>>
{
    public Guid UserId { get; set; }

    public GetUserSessionsQuery(Guid userId)
    {
        UserId = userId;
    }
}
