using MediatR;
using DATN.Application.Common.Models;
using DATN.Application.DTOs.Users;

namespace DATN.Application.Features.Users.Queries;

public class GetUserByIdQuery : IRequest<ApiResponse<UserDetailDto>>
{
    public Guid UserId { get; set; }

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
