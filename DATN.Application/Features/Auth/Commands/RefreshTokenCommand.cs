using MediatR;
using DATN.Application.DTOs.Auth;

namespace DATN.Application.Features.Auth.Commands;

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; }

    public RefreshTokenCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
