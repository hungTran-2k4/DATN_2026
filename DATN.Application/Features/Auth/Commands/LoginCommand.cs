using MediatR;
using MyProject.Application.Models.Auth;

namespace MyProject.Application.Features.Auth.Commands;

/// <summary>
/// MediatR Command cho đăng nhập
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
