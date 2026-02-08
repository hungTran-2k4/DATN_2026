using MediatR;
using MyProject.Application.Models.Auth;

namespace MyProject.Application.Features.Auth.Commands;

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; }

    public RefreshTokenCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
