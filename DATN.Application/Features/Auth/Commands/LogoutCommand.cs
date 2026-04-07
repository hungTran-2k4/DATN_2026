using DATN.Application.Common.Models;
using MediatR;

namespace DATN.Application.Features.Auth.Commands;

public class LogoutCommand : IRequest<ApiResponse<bool>>
{
    public string RefreshToken { get; set; }

    public LogoutCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
